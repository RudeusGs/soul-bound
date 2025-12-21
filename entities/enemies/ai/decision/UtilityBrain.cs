using Godot;

/// <summary>
/// UtilityBrain
///
/// Module ra quyết định (decision-making) theo mô hình Utility AI.
///
/// Ý tưởng cốt lõi:
/// - Mỗi hành động (Attack / Chase / Investigate / ReturnHome / Patrol) được chấm điểm 0..1
/// - Hành động có điểm cao nhất sẽ được chọn cho frame hiện tại
///
/// Vai trò trong kiến trúc:
/// - Đọc dữ liệu từ EnemyBlackboard (Target, Suspicion, LoseSightTimer, HasLastKnownPos...)
/// - Dựa trên bối cảnh hiện tại (khoảng cách tới target, combat range, vị trí home)
/// - Trả về UtilityAction để EnemyBrain quyết định chuyển state.
///
/// UtilityBrain KHÔNG:
/// - Không điều khiển movement/attack trực tiếp
/// - Không tự Change state
///   → EnemyBrain sẽ gọi StateMachine.Change() theo action trả về.
///
/// Chống flip-flop (dao động hành vi):
/// - Codebase đã có lock timer ở EnemyBrain.
/// - Riêng “leash/home” dùng hysteresis (EnterDist/ExitDist) để tránh rung khi ở gần ngưỡng.
///
/// Leash / ReturnHome (chống kiting):
/// - Khi Enemy đi quá xa khỏi home → bật LeashBroken:
///     + ReturnHome có điểm tuyệt đối (1.0) → bắt buộc quay về, không rượt nữa.
///     + Chase/Investigate bị chặn (score=0) để tránh chạy tới chạy về.
/// - Tuy nhiên để không bị người chơi “đánh ké”:
///     + Nếu LeashBroken và bị đánh → cho phép “phản kích ngắn” bằng RetaliateTimer.
///     + Phản kích chỉ xảy ra nếu attacker nằm trong tầm đánh (combat range).
/// </summary>
public sealed class UtilityBrain
{
    /// <summary>
    /// Enemy owner – dùng lấy vị trí hiện tại (GlobalPosition).
    /// </summary>
    private readonly Enemy _enemy;

    /// <summary>
    /// Blackboard – dữ liệu dùng chung của AI (target, suspicion, timers...).
    /// </summary>
    private readonly EnemyBlackboard _bb;

    /// <summary>
    /// Module combat – dùng kiểm tra target có trong tầm đánh không.
    /// </summary>
    private readonly EnemyCombat _combat;

    /// <summary>
    /// Vị trí home (thường là vị trí spawn ban đầu).
    /// Dùng làm mốc leash và quay về.
    /// </summary>
    private readonly Vector2 _homePos;

    /// <summary>
    /// Ngưỡng khoảng cách tối đa để bắt đầu ưu tiên ReturnHome
    /// (khi chưa LeashBroken, ReturnHome tăng dần theo độ xa).
    /// </summary>
    private readonly float _maxHomeDist;

    /// <summary>
    /// Khởi tạo UtilityBrain.
    ///
    /// enemy       : Enemy owner
    /// bb          : Blackboard
    /// combat      : Module combat
    /// homePos     : vị trí home/spawn
    /// maxHomeDist : ngưỡng “đi xa nhà” để ReturnHome bắt đầu có điểm
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
    /// Luồng hoạt động:
    /// 1) Early exit: chết thì Idle.
    /// 2) Update leash (EnterDist/ExitDist) để quyết định có “bắt buộc quay về” hay không.
    /// 3) Tính score cho từng hành động.
    /// 4) Chọn hành động có score cao nhất và trả về.
    ///
    /// Thứ tự ưu tiên thực tế (thường gặp):
    /// - Attack: thắng khi target trong range (hoặc retaliate khi leash broken)
    /// - Chase: khi còn target hoặc còn LoseSightTimer
    /// - Investigate: khi còn LastKnownPos và suspicion/damageAwareness đủ lớn
    /// - ReturnHome: khi đi xa nhà (đặc biệt khi LeashBroken → bắt buộc về)
    /// - Patrol: nền khi không có target và suspicion thấp
    /// </summary>
    public UtilityAction Decide()
    {
        // Dead → không làm gì
        if (_bb.IsDead) return UtilityAction.Idle;

        // ===== 1) LEASH CONTROL (hysteresis) =====
        // distHome: khoảng cách hiện tại từ enemy tới home
        float distHome = _enemy.GlobalPosition.DistanceTo(_homePos);

        // Enter: vượt ngưỡng -> bật leash broken (bắt buộc quay về)
        if (!_bb.LeashBroken && distHome >= _bb.LeashEnterDist)
            _bb.LeashBroken = true;

        // Exit: chỉ tắt leash khi về đủ gần home (tránh rung qua lại gần ngưỡng)
        else if (_bb.LeashBroken && distHome <= _bb.LeashExitDist)
            _bb.LeashBroken = false;

        // ===== 2) SCORE EACH ACTION =====
        float sAttack = ScoreAttack();
        float sChase = ScoreChase();
        float sInv = ScoreInvestigate();
        float sHome = ScoreReturnHome();
        float sPatrol = ScorePatrol();

        // ===== 3) PICK BEST =====
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
    /// Pick()
    ///
    /// Helper: cập nhật action tốt nhất nếu score mới lớn hơn bestScore.
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
    /// Attack có điểm khi:
    /// - Có target hợp lệ (Target != null && IsInsideTree)
    /// - Và target trong combat range (EnemyCombat.IsInRange)
    ///
    /// Hai chế độ:
    /// A) Normal mode (LeashBroken = false):
    /// - Chỉ attack khi RequestAttack = true (tức là player đang ở AttackRangeArea)
    /// - Score gần như “chắc chắn thắng” để vào AttackState ổn định.
    ///
    /// B) LeashBroken mode (đang quay về):
    /// - Tuyệt đối không chase, nhưng cho phép “phản kích ngắn” để tránh bị đánh ké.
    /// - Phản kích chỉ diễn ra trong cửa sổ RetaliateTimer (>0) và target phải trong tầm đánh.
    /// </summary>
    private float ScoreAttack()
    {
        // Không có target hợp lệ → không thể attack
        if (_bb.Target == null || !_bb.Target.IsInsideTree())
            return 0f;

        // ===== LeashBroken: chỉ retaliate (phản kích ngắn) =====
        if (_bb.LeashBroken)
        {
            // Hết thời gian phản kích → không attack
            if (_bb.RetaliateTimer <= 0) return 0f;

            // Chỉ phản kích nếu đủ gần (trong tầm đánh)
            if (!_combat.IsInRange(_bb.Target)) return 0f;

            // Phản kích dứt khoát
            return 1f;
        }

        // ===== Normal: chỉ attack khi player ở AttackRangeArea =====
        if (!_bb.RequestAttack) return 0f;

        // Safety: phải trong tầm đánh
        if (!_combat.IsInRange(_bb.Target)) return 0f;

        // Score cao để Attack thắng Chase, tránh flip-flop
        return Mathf.Clamp(0.95f + 0.05f * _bb.Suspicion, 0f, 1f);
    }

    /// <summary>
    /// ScoreChase()
    ///
    /// Chase có điểm khi:
    /// - Có target hợp lệ, hoặc
    /// - Vừa mới mất target nhưng còn grace period (LoseSightTimer > 0)
    ///
    /// Nếu LeashBroken:
    /// - Chase bị chặn hoàn toàn (0) để “bắt buộc quay về”, chống chạy tới chạy về.
    ///
    /// Score dựa trên Suspicion (nghi ngờ càng cao → chase càng quyết).
    /// </summary>
    private float ScoreChase()
    {
        // LeashBroken → tuyệt đối không chase
        if (_bb.LeashBroken) return 0f;

        bool hasTarget = _bb.Target != null && _bb.Target.IsInsideTree();
        if (!hasTarget && _bb.LoseSightTimer <= 0) return 0f;

        // Baseline 0.3 để chase vẫn có động lực trong grace period
        return Mathf.Clamp(0.7f * _bb.Suspicion + 0.3f, 0f, 1f);
    }

    /// <summary>
    /// ScoreInvestigate()
    ///
    /// Investigate có điểm khi:
    /// - Có LastKnownTargetPos (HasLastKnownPos = true)
    ///
    /// Nếu LeashBroken:
    /// - Investigate bị chặn (0) để ưu tiên ReturnHome, tránh dao động.
    ///
    /// Score = kết hợp Suspicion và DamageAwareness.
    /// </summary>
    private float ScoreInvestigate()
    {
        if (_bb.LeashBroken) return 0f;
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
    /// ReturnHome có điểm khi:
    /// - Đi quá xa home (distHome >= _maxHomeDist)
    ///
    /// Nếu LeashBroken:
    /// - ReturnHome thắng tuyệt đối (1.0) để dứt khoát quay về.
    ///
    /// Nếu chưa LeashBroken:
    /// - Score tăng theo độ xa nhà và mức “ít nghi ngờ” (lowSusp).
    /// - Suspicion thấp → dễ bỏ cuộc quay về.
    /// </summary>
    private float ScoreReturnHome()
    {
        float distHome = _enemy.GlobalPosition.DistanceTo(_homePos);

        // LeashBroken → bắt buộc quay về
        if (_bb.LeashBroken) return 1f;

        if (distHome < _maxHomeDist) return 0f;

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
    /// - Không “đè” lên Chase/Investigate khi suspicion còn cao.
    /// </summary>
    private float ScorePatrol()
    {
        // Có target thì không patrol
        if (_bb.Target != null && _bb.Target.IsInsideTree()) return 0f;

        float lowSusp = 1f - _bb.Suspicion;
        return Mathf.Clamp(0.4f * lowSusp, 0f, 0.4f);
    }
}
