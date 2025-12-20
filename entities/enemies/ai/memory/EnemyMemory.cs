using Godot;

/// <summary>
/// EnemyMemory
///
/// Module quản lý "trí nhớ" và trạng thái tâm lý của Enemy AI.
///
/// Vai trò:
/// - Ghi nhận các sự kiện perception (See / Hear / Damaged)
/// - Duy trì và decay các giá trị tâm lý (Suspicion, Alertness)
/// - Quản lý thông tin mục tiêu:
///     + Target hiện tại
///     + LastKnownTargetPos
///     + LoseSightTimer (grace period)
///
/// EnemyMemory KHÔNG:
/// - Không điều khiển movement
/// - Không quyết định state (Patrol / Chase / Attack...)
/// - Không xử lý animation
///
/// Thiết kế này tuân theo luồng:
/// Perception → EnemyMemory → Decision (Utility / FSM)
/// </summary>
public sealed class EnemyMemory
{
    /// <summary>
    /// Blackboard – nơi lưu toàn bộ dữ liệu dùng chung của Enemy.
    /// EnemyMemory đọc/ghi dữ liệu vào đây.
    /// </summary>
    private readonly EnemyBlackboard _bb;

    /// <summary>
    /// Cấu hình trí nhớ (data-driven).
    /// Cho phép tinh chỉnh hành vi AI qua Inspector.
    /// </summary>
    private readonly MemoryConfig _cfg;

    /// <summary>
    /// Khởi tạo EnemyMemory.
    ///
    /// bb  : Blackboard dùng chung của Enemy
    /// cfg : MemoryConfig (Resource)
    /// </summary>
    public EnemyMemory(EnemyBlackboard bb, MemoryConfig cfg)
    {
        _bb = bb;
        _cfg = cfg;
    }

    /// <summary>
    /// Được gọi mỗi frame để:
    /// - Decay Suspicion / Alertness
    /// - Cập nhật các timer (seen / heard / lose sight)
    /// - Xoá target không còn hợp lệ
    /// - Quên vị trí nghi ngờ sau một thời gian
    /// </summary>
    public void Tick(double delta)
    {
        float d = (float)delta;

        // 1) Decay "tâm lý"
        _bb.Suspicion = Mathf.Max(0f, _bb.Suspicion - _cfg.SuspicionDecayPerSec * d);
        _bb.Alertness = Mathf.Max(0f, _bb.Alertness - _cfg.AlertnessDecayPerSec * d);

        // 2) Update timers
        _bb.TimeSinceLastSeen += delta;
        _bb.TimeSinceLastHeard += delta;

        // 3) Grace period: mất sight vẫn còn "đuổi theo trí nhớ"
        if (_bb.LoseSightTimer > 0)
            _bb.LoseSightTimer = Mathf.Max(0f, (float)(_bb.LoseSightTimer - delta));

        // 4) Nếu target bị destroy / không còn trong scene tree → bỏ target
        if (_bb.Target != null && !_bb.Target.IsInsideTree())
            _bb.Target = null;

        // 5) Quên vị trí cuối cùng sau một khoảng thời gian
        bool forgetBySeen = _bb.TimeSinceLastSeen >= _cfg.ForgetPosAfterSec;
        bool forgetByHeard = _bb.TimeSinceLastHeard >= _cfg.ForgetPosAfterSec;

        if (_bb.HasLastKnownPos && forgetBySeen && forgetByHeard)
            _bb.HasLastKnownPos = false;
    }

    /// <summary>
    /// Được gọi khi Enemy NHÌN THẤY một thực thể.
    ///
    /// Hành vi:
    /// - Lock target
    /// - Cập nhật LastKnownTargetPos
    /// - Reset LoseSightTimer
    /// - Tăng Suspicion / Alertness
    /// </summary>
    public void OnSee(Node2D actor, Vector2 pos, float strength)
    {
        if (actor == null) return;

        // Lock target + update last known position
        _bb.Target = actor;
        _bb.LastKnownTargetPos = pos;
        _bb.HasLastKnownPos = true;

        // Reset grace timer
        _bb.LoseSightTimer = 2.0f;

        // Tăng suspicion / alertness
        _bb.Suspicion = Mathf.Clamp(
            _bb.Suspicion + _cfg.SuspicionGainOnSee * strength,
            0f, 1f);

        _bb.Alertness = Mathf.Clamp(
            _bb.Alertness + 0.5f * strength,
            0f, 1f);

        // Reset timer seen
        _bb.TimeSinceLastSeen = 0;
    }

    /// <summary>
    /// Được gọi khi Enemy NGHE THẤY âm thanh.
    ///
    /// Hành vi:
    /// - Không lock target
    /// - Chỉ cập nhật LastKnownTargetPos
    /// - Tăng Suspicion / Alertness ở mức thấp hơn vision
    /// </summary>
    public void OnHear(Vector2 pos, float strength)
    {
        _bb.LastKnownTargetPos = pos;
        _bb.HasLastKnownPos = true;

        _bb.Suspicion = Mathf.Clamp(
            _bb.Suspicion + _cfg.SuspicionGainOnHear * strength,
            0f, 1f);

        _bb.Alertness = Mathf.Clamp(
            _bb.Alertness + 0.25f * strength,
            0f, 1f);

        _bb.TimeSinceLastHeard = 0;
    }

    /// <summary>
    /// Được gọi khi Enemy bị tấn công (Damaged).
    ///
    /// Hành vi:
    /// - Tăng mạnh Suspicion / Alertness
    /// - Nếu biết attacker → coi như "thấy" để lock nhanh
    /// - Nếu không biết attacker → chỉ nhớ vị trí bị đánh
    ///
    /// Dùng cho:
    /// - Backstab
    /// - Đánh lén
    /// - Stealth gameplay
    /// </summary>
    public void OnDamaged(Node2D attacker, Vector2 hitPos, float strength = 1.0f)
    {
        _bb.Suspicion = Mathf.Clamp(_bb.Suspicion + 0.9f * strength, 0f, 1f);
        _bb.Alertness = Mathf.Clamp(_bb.Alertness + 0.7f * strength, 0f, 1f);

        // Nếu attacker hợp lệ → lock nhanh
        if (attacker != null && attacker.IsInsideTree())
        {
            OnSee(attacker, attacker.GlobalPosition, strength);
            return;
        }

        // Không rõ attacker → chỉ biết vị trí bị đánh
        _bb.LastKnownTargetPos = hitPos;
        _bb.HasLastKnownPos = true;
        _bb.TimeSinceLastHeard = 0;

        // Cho một khoảng grace để investigate
        _bb.LoseSightTimer = Mathf.Max(_bb.LoseSightTimer, 0.8f);
    }
}
