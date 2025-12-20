using Godot;

/// <summary>
/// UtilityBrain
///
/// Module ra quyết định (decision-making) theo mô hình Utility AI.
///
/// Ý tưởng:
/// - Mỗi hành động (Attack / Chase / Investigate / ReturnHome / Patrol) được chấm điểm 0..1
/// - Hành động có điểm cao nhất sẽ được chọn cho frame hiện tại
///
/// Vai trò:
/// - Đọc dữ liệu từ EnemyBlackboard (Target, Suspicion, LoseSightTimer, LastKnownPos...)
/// - Dựa trên bối cảnh hiện tại (khoảng cách, combat range, vị trí home)
/// - Trả về UtilityAction "nên làm gì"
///
/// UtilityBrain KHÔNG:
/// - Không thực thi movement/attack trực tiếp
/// - Không chuyển state trực tiếp
///   → EnemyBrain là nơi gọi StateMachine.Change() theo action được trả về
///
/// Lưu ý quan trọng khi dùng Utility AI:
/// - Vì điểm số thay đổi theo thời gian, có thể xảy ra flip-flop (đổi action liên tục)
///   → Cần StateMachine chống đổi lặp và EnemyBrain có debounce/lock timer.
/// </summary>
public sealed class UtilityBrain
{
    /// <summary>
    /// Enemy owner, dùng để lấy vị trí hiện tại.
    /// </summary>
    private readonly Enemy _enemy;

    /// <summary>
    /// Blackboard – dữ liệu dùng chung cho AI (target, suspicion, timers...).
    /// </summary>
    private readonly EnemyBlackboard _bb;

    /// <summary>
    /// Module combat – dùng để kiểm tra phạm vi tấn công.
    /// </summary>
    private readonly EnemyCombat _combat;

    /// <summary>
    /// Vị trí home (thường là vị trí spawn ban đầu).
    /// Dùng để giới hạn phạm vi roaming và quyết định quay về.
    /// </summary>
    private readonly Vector2 _homePos;

    /// <summary>
    /// Khoảng cách tối đa Enemy được phép đi xa khỏi home.
    /// Nếu vượt quá, UtilityBrain sẽ bắt đầu ưu tiên ReturnHome.
    /// </summary>
    private readonly float _maxHomeDist;

    /// <summary>
    /// Khởi tạo UtilityBrain.
    ///
    /// enemy       : Enemy owner
    /// bb          : Blackboard
    /// combat      : Module combat
    /// homePos     : Vị trí home
    /// maxHomeDist : Giới hạn đi xa khỏi home
    /// </summary>
    public UtilityBrain(Enemy enemy, EnemyBlackboard bb, EnemyCombat combat, Vector2 homePos, float maxHomeDist = 400f)
    {
        _enemy = enemy;
        _bb = bb;
        _combat = combat;
        _homePos = homePos;
        _maxHomeDist = maxHomeDist;
    }

    /// <summary>
    /// Decide()
    ///
    /// Tính điểm cho từng hành động và chọn hành động có điểm cao nhất.
    ///
    /// Thứ tự ưu tiên thực tế:
    /// - Attack thường thắng khi target ở trong range
    /// - Chase khi còn target hoặc còn LoseSightTimer
    /// - Investigate khi có LastKnownTargetPos và suspicion/damageAwareness đủ lớn
    /// - ReturnHome khi đi quá xa và suspicion thấp
    /// - Patrol khi không có target và suspicion thấp
    ///
    /// Trả về:
    /// - UtilityAction tương ứng để EnemyBrain chuyển state.
    /// </summary>
    public UtilityAction Decide()
    {
        // Dead → không làm gì
        if (_bb.IsDead) return UtilityAction.Idle;

        float sAttack = ScoreAttack();
        float sChase = ScoreChase();
        float sInv = ScoreInvestigate();
        float sHome = ScoreReturnHome();
        float sPatrol = ScorePatrol();

        // Chọn action có score lớn nhất
        var best = UtilityAction.Idle;
        float bestScore = 0f;

        Pick(ref best, ref bestScore, UtilityAction.Attack, sAttack);
        Pick(ref best, ref bestScore, UtilityAction.Chase, sChase);
        Pick(ref best, ref bestScore, UtilityAction.Investigate, sInv);
        Pick(ref best, ref bestScore, UtilityAction.ReturnHome, sHome);
        Pick(ref best, ref bestScore, UtilityAction.Patrol, sPatrol);

        return best;
    }

    /// <summary>
    /// Helper: cập nhật action tốt nhất nếu score s > bestScore.
    /// </summary>
    private void Pick(ref UtilityAction best, ref float bestScore, UtilityAction a, float s)
    {
        if (s > bestScore)
        {
            best = a;
            bestScore = s;
        }
    }

    /// <summary>
    /// ScoreAttack()
    ///
    /// Chỉ có điểm khi:
    /// - Có target hợp lệ
    /// - Target nằm trong tầm đánh của EnemyCombat
    ///
    /// Score phụ thuộc vào Suspicion:
    /// - Suspicion càng cao → càng "quyết đánh"
    /// </summary>
    private float ScoreAttack()
    {
        if (_bb.Target == null || !_bb.Target.IsInsideTree()) return 0f;
        if (!_combat.IsInRange(_bb.Target)) return 0f;

        // Càng nghi ngờ cao càng dễ đánh (curve làm tăng tính dứt khoát)
        return Curves.Sharp(_bb.Suspicion);
    }

    /// <summary>
    /// ScoreChase()
    ///
    /// Có điểm khi:
    /// - Có target hợp lệ, hoặc
    /// - Vừa mới mất target nhưng còn trong grace period (LoseSightTimer > 0)
    ///
    /// Nếu không còn target và LoseSightTimer <= 0 → không chase.
    ///
    /// Score dựa trên Suspicion:
    /// - Suspicion cao → chase quyết liệt hơn
    /// </summary>
    private float ScoreChase()
    {
        bool hasTarget = _bb.Target != null && _bb.Target.IsInsideTree();
        if (!hasTarget && _bb.LoseSightTimer <= 0) return 0f;

        // Nếu không còn target nhưng còn timer => chase theo trí nhớ (LastKnownTargetPos)
        return Mathf.Clamp(0.7f * _bb.Suspicion + 0.3f, 0f, 1f);
    }

    /// <summary>
    /// ScoreInvestigate()
    ///
    /// Có điểm khi:
    /// - Có LastKnownTargetPos (HasLastKnownPos = true)
    ///
    /// Score dựa trên:
    /// - Suspicion: mức nghi ngờ tổng quát
    /// - DamageAwareness: mức "cảnh giác do bị đánh" / bị tấn công bất ngờ
    ///
    /// Lưu ý:
    /// - Nếu DamageAwareness không decay theo thời gian, Investigate có thể kéo dài
    ///   → nên decay DamageAwareness trong EnemyMemory.Tick (nếu dùng biến này).
    /// </summary>
    private float ScoreInvestigate()
    {
        if (!_bb.HasLastKnownPos) return 0f;

        return Mathf.Clamp(
            0.6f * _bb.Suspicion +
            0.4f * _bb.DamageAwareness,
            0f, 1f
        );
    }

    /// <summary>
    /// ScoreReturnHome()
    ///
    /// Có điểm khi Enemy đi quá xa home (_maxHomeDist).
    /// Khi đó, nếu Suspicion thấp thì ưu tiên quay về.
    ///
    /// Score = (độ "ít nghi ngờ") * (độ "xa nhà")
    /// - Suspicion càng thấp → càng dễ bỏ cuộc quay về
    /// - DistHome càng xa → score càng cao
    /// </summary>
    private float ScoreReturnHome()
    {
        float distHome = _enemy.GlobalPosition.DistanceTo(_homePos);
        if (distHome < _maxHomeDist) return 0f;

        // Nếu nghi ngờ thấp mà đi quá xa => quay về
        float lowSusp = 1f - _bb.Suspicion;
        float far = Curves.InverseLerp(_maxHomeDist, _maxHomeDist * 1.5f, distHome);
        return Mathf.Clamp(lowSusp * far, 0f, 1f);
    }

    /// <summary>
    /// ScorePatrol()
    ///
    /// Patrol là hành vi nền khi:
    /// - Không có target
    /// - Suspicion thấp
    ///
    /// Score bị giới hạn tối đa 0.4 để:
    /// - Không "đè" lên Investigate/Chase khi suspicion còn cao
    /// - Patrol chỉ thắng khi các hành động khác gần như 0
    /// </summary>
    private float ScorePatrol()
    {
        // Patrol khi không target + nghi ngờ thấp
        if (_bb.Target != null && _bb.Target.IsInsideTree()) return 0f;

        float lowSusp = 1f - _bb.Suspicion;
        return Mathf.Clamp(0.4f * lowSusp, 0f, 0.4f);
    }
}
