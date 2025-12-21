using Godot;
using System;

/// <summary>
/// EnemyMemory
///
/// Module "trí nhớ" + trạng thái tâm lý của Enemy AI.
///
/// Mục tiêu:
/// - Biến các tín hiệu Perception (nhìn/nghe/bị đánh) thành dữ liệu bền hơn theo thời gian.
/// - Giữ thông tin mục tiêu (Target, LastKnownTargetPos) và các timer liên quan.
/// - Quản lý các biến tâm lý (Suspicion, Alertness, DamageAwareness...) và cho chúng decay.
///
/// Vai trò trong kiến trúc:
/// Perception (VisionSensor/Audio/Hit) → EnemyMemory (ghi bb) → UtilityBrain (đọc bb quyết định) → FSM (thực thi)
///
/// EnemyMemory KHÔNG:
/// - Không điều khiển movement.
/// - Không quyết định state.
/// - Không xử lý animation.
///
/// Lưu ý:
/// - Các biến "tâm lý" luôn nên decay theo thời gian để AI không bị "kích động vĩnh viễn".
/// - Các timer (LoseSightTimer, TimeSinceLastSeen/Heard) là nền để Chase/Investigate có hành vi "giống người".
/// - RetaliateTimer: dùng cho cơ chế phản kích ngắn khi LeashBroken (đang quay về home).
/// </summary>
public sealed class EnemyMemory
{
    /// <summary>
    /// Blackboard – nơi lưu toàn bộ dữ liệu dùng chung của Enemy.
    /// EnemyMemory chỉ thao tác trên bb, không giữ state riêng phức tạp.
    /// </summary>
    private readonly EnemyBlackboard _bb;

    /// <summary>
    /// MemoryConfig – cấu hình data-driven (Resource) để tinh chỉnh AI qua Inspector:
    /// - tốc độ decay suspicion/alertness
    /// - mức gain khi see/hear
    /// - thời gian quên vị trí nghi ngờ
    /// </summary>
    private readonly MemoryConfig _cfg;

    /// <summary>
    /// Khởi tạo EnemyMemory.
    /// bb  : Blackboard dùng chung của Enemy
    /// cfg : MemoryConfig (Resource)
    /// </summary>
    public EnemyMemory(EnemyBlackboard bb, MemoryConfig cfg)
    {
        _bb = bb;
        _cfg = cfg;
    }

    /// <summary>
    /// Tick(delta)
    ///
    /// Được gọi mỗi frame để:
    /// 1) Decay các giá trị tâm lý (Suspicion/Alertness)
    /// 2) Cập nhật timer (seen/heard/lose-sight, retaliate)
    /// 3) Dọn dẹp target không còn hợp lệ
    /// 4) Quên last known position sau một khoảng thời gian
    ///
    /// Ghi chú:
    /// - Tick chỉ làm "bảo trì" dữ liệu trí nhớ, không ra quyết định.
    /// - Việc "chọn hành động" thuộc UtilityBrain.
    /// </summary>
    public void Tick(double delta)
    {
        float d = (float)delta;

        // ===== 1) DECAY "TÂM LÝ" =====
        // Suspicion/Alertness giảm dần theo thời gian để AI dịu lại.
        _bb.Suspicion = Mathf.Max(0f, _bb.Suspicion - _cfg.SuspicionDecayPerSec * d);
        _bb.Alertness = Mathf.Max(0f, _bb.Alertness - _cfg.AlertnessDecayPerSec * d);

        // ===== 2) UPDATE TIMERS =====
        // Các timer giúp AI hiểu "mới thấy / mới nghe" hay đã lâu rồi.
        _bb.TimeSinceLastSeen += delta;
        _bb.TimeSinceLastHeard += delta;

        // LoseSightTimer: "grace period" sau khi mất sight.
        // Trong thời gian này, UtilityBrain vẫn có thể chọn Chase/Investigate theo trí nhớ.
        if (_bb.LoseSightTimer > 0)
            _bb.LoseSightTimer = Mathf.Max(0f, (float)(_bb.LoseSightTimer - delta));

        // RetaliateTimer: cửa sổ phản kích ngắn khi đang LeashBroken (đang quay về home).
        // Khi timer về 0, ScoreAttack (leash mode) sẽ trả 0 => quay lại ReturnHome.
        if (_bb.RetaliateTimer > 0)
            _bb.RetaliateTimer = Math.Max(0, _bb.RetaliateTimer - delta);

        // ===== 3) VALIDATE TARGET =====
        // Nếu target bị destroy / không còn trong scene tree → bỏ target để tránh null ref / chase "ma".
        if (_bb.Target != null && !_bb.Target.IsInsideTree())
            _bb.Target = null;

        // ===== 4) FORGET LAST KNOWN POSITION =====
        // Quên vị trí nghi ngờ nếu đã quá lâu kể từ lần thấy/nghe gần nhất.
        // Điều kiện both seen & heard đều đã quá hạn => mới quên.
        bool forgetBySeen = _bb.TimeSinceLastSeen >= _cfg.ForgetPosAfterSec;
        bool forgetByHeard = _bb.TimeSinceLastHeard >= _cfg.ForgetPosAfterSec;

        if (_bb.HasLastKnownPos && forgetBySeen && forgetByHeard)
            _bb.HasLastKnownPos = false;
    }

    /// <summary>
    /// OnSee(actor, pos, strength)
    ///
    /// Được gọi khi Enemy "nhìn thấy" một thực thể.
    ///
    /// Hành vi:
    /// - Lock target ngay lập tức.
    /// - Cập nhật LastKnownTargetPos.
    /// - Reset LoseSightTimer (grace period) để Chase không rớt target ngay.
    /// - Tăng Suspicion/Alertness theo strength (độ rõ của perception).
    /// - Reset TimeSinceLastSeen.
    /// </summary>
    public void OnSee(Node2D actor, Vector2 pos, float strength)
    {
        if (actor == null) return;

        // 1) Lock target + update last known pos
        _bb.Target = actor;
        _bb.LastKnownTargetPos = pos;
        _bb.HasLastKnownPos = true;

        // 2) Reset grace timer (mất sight vẫn còn đuổi thêm một đoạn)
        _bb.LoseSightTimer = 4.0f;

        // 3) Gain tâm lý
        _bb.Suspicion = Mathf.Clamp(
            _bb.Suspicion + _cfg.SuspicionGainOnSee * strength,
            0f, 1f);

        _bb.Alertness = Mathf.Clamp(
            _bb.Alertness + 0.5f * strength,
            0f, 1f);

        // 4) Reset timer seen
        _bb.TimeSinceLastSeen = 0;
    }

    /// <summary>
    /// OnHear(pos, strength)
    ///
    /// Được gọi khi Enemy "nghe thấy" âm thanh (không chắc chắn mục tiêu).
    ///
    /// Hành vi:
    /// - Không lock target (vì hearing thường không xác định chính xác actor).
    /// - Chỉ ghi nhận vị trí nghi ngờ LastKnownTargetPos.
    /// - Tăng Suspicion/Alertness ít hơn so với vision.
    /// - Reset TimeSinceLastHeard.
    /// </summary>
    public void OnHear(Vector2 pos, float strength)
    {
        // Hearing chỉ tạo "điểm nghi ngờ" để Investigate
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
    /// OnDamaged(attacker, hitPos, strength)
    ///
    /// Được gọi khi Enemy bị tấn công.
    ///
    /// Hành vi:
    /// - Tăng mạnh Suspicion/Alertness (bị đánh => cảnh giác cao).
    /// - Nếu biết attacker hợp lệ:
    ///     + coi như "thấy" để lock target nhanh (gọi lại OnSee)
    /// - Nếu không biết attacker:
    ///     + chỉ ghi nhận vị trí bị đánh (hitPos) để Investigate
    ///     + đặt LoseSightTimer tối thiểu để có thời gian investigate
    ///
    /// Dùng cho gameplay:
    /// - đánh lén/backstab
    /// - stealth (đánh mà enemy không thấy rõ ai)
    /// </summary>
    public void OnDamaged(Node2D attacker, Vector2 hitPos, float strength = 1.0f)
    {
        // 1) Gain tâm lý mạnh hơn see/hear
        _bb.Suspicion = Mathf.Clamp(_bb.Suspicion + 0.9f * strength, 0f, 1f);
        _bb.Alertness = Mathf.Clamp(_bb.Alertness + 0.7f * strength, 0f, 1f);

        // 2) Nếu attacker hợp lệ → lock nhanh bằng vision logic
        if (attacker != null && attacker.IsInsideTree())
        {
            OnSee(attacker, attacker.GlobalPosition, strength);
            return;
        }

        // 3) Không rõ attacker → chỉ biết vị trí bị đánh
        _bb.LastKnownTargetPos = hitPos;
        _bb.HasLastKnownPos = true;
        _bb.TimeSinceLastHeard = 0;

        // 4) Cho một khoảng grace tối thiểu để Investigate
        _bb.LoseSightTimer = Mathf.Max(_bb.LoseSightTimer, 0.8f);
    }
}
