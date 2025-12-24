using Godot;

/// <summary>
/// EnemyBlackboard
///
/// "Bảng ghi nhớ" (shared memory) của Enemy AI.
///
/// Mục tiêu:
/// - Là nơi lưu toàn bộ dữ liệu dùng chung giữa các module:
///   + Perception (Vision/AttackRange/Hit) ghi vào đây
///   + EnemyMemory đọc/ghi và decay timer/psychology
///   + UtilityBrain đọc để chấm điểm hành động
///   + FSM/States ghi các cờ IsChasing/IsAttacking... khi thực thi
///   + EnemyAnimation đọc Facing/IsAttacking/IsChasing để chọn sprite
///
/// Nguyên tắc:
/// - Blackboard chỉ chứa dữ liệu, KHÔNG chứa logic.
/// - Tránh để nhiều module cùng "định nghĩa ý nghĩa" một biến.
///   → comment dưới đây ghi rõ biến chủ yếu do module nào set/đọc.
/// </summary>
public sealed class EnemyBlackboard
{
    // =========================================================
    // 1) Targeting / Orientation (mục tiêu & hướng nhìn)
    // =========================================================

    /// <summary>
    /// Target hiện tại (đối tượng Enemy đang lock).
    /// - Ghi bởi: EnemyMemory.OnSee, AttackRangeSensor (khi vào range), EnemyBrain.OnDamaged
    /// - Đọc bởi: UtilityBrain (score), Chase/Attack states, EnemyAnimation (UpdateFacingFromTargetIfAny)
    /// </summary>
    /// 
    public bool AttackFacingLocked = false;
    public FacingDir AttackFacing = FacingDir.Down;
    public Node2D Target;

    /// <summary>
    /// Hướng nhìn hiện tại (Up/Down/Left/Right).
    /// - Ghi bởi: EnemyAnimation (khi di chuyển) hoặc khi đang attack (hướng theo target)
    /// - Đọc bởi: EnemyAnimation để map suffix anim
    /// </summary>
    public FacingDir Facing = FacingDir.Down;

    // =========================================================
    // 2) Perception Evidence / Memory (bằng chứng nhận thức)
    // =========================================================

    /// <summary>
    /// Vị trí cuối cùng biết được của target (last known position).
    /// - Ghi bởi: EnemyMemory (OnSee/OnHear/OnDamaged), VisionSensor khi mất sight
    /// - Đọc bởi: InvestigateState, ChaseState (đuổi theo trí nhớ)
    /// </summary>
    public Vector2 LastKnownTargetPos;

    /// <summary>
    /// Có đang có LastKnownTargetPos hợp lệ không.
    /// - Ghi bởi: EnemyMemory/Vision logic (set true khi có evidence, set false khi quên)
    /// - Đọc bởi: UtilityBrain (ScoreInvestigate), States (Investigate/Chase)
    /// </summary>
    public bool HasLastKnownPos;

    /// <summary>
    /// Grace period sau khi mất sight.
    /// Trong thời gian này vẫn có thể Chase theo trí nhớ ngay cả khi Target bị null.
    /// - Giảm dần bởi: EnemyMemory.Tick
    /// - Đọc bởi: UtilityBrain.ScoreChase
    /// </summary>
    public double LoseSightTimer;

    // =========================================================
    // 3) Psychological State (tâm lý / mức cảnh giác)
    // =========================================================

    /// <summary>
    /// Mức nghi ngờ 0..1 (cao -> ưu tiên chase/attack mạnh).
    /// - Tăng bởi: EnemyMemory.OnSee/OnHear/OnDamaged, EnemyBrain.OnDamaged
    /// - Giảm dần bởi: EnemyMemory.Tick (decay)
    /// - Đọc bởi: UtilityBrain để tính score
    /// </summary>
    public float Suspicion;

    /// <summary>
    /// Mức cảnh giác tổng quát 0..1.
    /// - Tăng bởi: EnemyMemory/OnDamaged
    /// - Giảm dần bởi: EnemyMemory.Tick
    /// - Có thể dùng để mở rộng logic (FOV, reaction time...) nếu cần
    /// </summary>
    public float Alertness;

    /// <summary>
    /// Nhận thức bị tấn công / awareness do damage.
    /// - Tăng bởi: EnemyBrain.OnDamaged (hoặc EnemyMemory.OnDamaged nếu bạn gom)
    /// - Đọc bởi: UtilityBrain.ScoreInvestigate
    /// Gợi ý: nên decay theo thời gian (nếu dùng lâu dài) để tránh investigate mãi.
    /// </summary>
    public float DamageAwareness;

    // =========================================================
    // 4) Runtime State Flags (FSM/Combat/Animation dùng)
    // =========================================================

    /// <summary>
    /// Enemy hiện đang ở trạng thái Chase (để animation chọn run).
    /// - Set bởi: ChaseState (Enter/Exit hoặc Tick)
    /// - Đọc bởi: EnemyAnimation
    /// </summary>
    public bool IsChasing;

    /// <summary>
    /// Enemy hiện đang tấn công (để animation chuyển sang attack).
    /// - Set bởi: AttackState (Enter/Exit hoặc theo attack window)
    /// - Đọc bởi: EnemyAnimation, Vision logic (tránh clear target giữa nhịp đánh)
    /// </summary>
    public bool IsAttacking;
    public bool InCombat;

    /// <summary>
    /// Enemy đã chết.
    /// - Set bởi: combat/health system
    /// - Đọc bởi: UtilityBrain (early exit), animation/state machine
    /// </summary>
    public bool IsDead;

    // =========================================================
    // 5) Timers / Timestamps (phục vụ trí nhớ & hành vi)
    // =========================================================

    /// <summary>
    /// Thời gian kể từ lần cuối nhìn thấy target.
    /// - Update bởi: EnemyMemory.Tick
    /// - Reset bởi: EnemyMemory.OnSee
    /// - Dùng để quyết định quên LastKnownPos
    /// </summary>
    public double TimeSinceLastSeen;

    /// <summary>
    /// Thời gian kể từ lần cuối nghe thấy dấu hiệu.
    /// - Update bởi: EnemyMemory.Tick
    /// - Reset bởi: EnemyMemory.OnHear / OnDamaged (khi chỉ biết vị trí)
    /// - Dùng để quyết định quên LastKnownPos
    /// </summary>
    public double TimeSinceLastHeard;

    // =========================================================
    // 6) Requests / Intents (tín hiệu điều kiện từ sensor)
    // =========================================================
    // Note: Các biến Request* thường là "input" cho UtilityBrain chứ không phải state.
    // Ví dụ: RequestAttack được AttackRangeSensor bật/tắt dựa vào vùng Area2D.

    /// <summary>
    /// Yêu cầu investigate (nếu bạn có sensor/logic riêng).
    /// Hiện tại UtilityBrain chủ yếu dùng HasLastKnownPos + suspicion,
    /// nên biến này có thể là dự phòng/mở rộng.
    /// </summary>
    public bool RequestInvestigate;

    /// <summary>
    /// Yêu cầu chase (dự phòng/mở rộng).
    /// Hiện tại UtilityBrain tính chase dựa trên Target + LoseSightTimer.
    /// </summary>
    public bool RequestChase;

    /// <summary>
    /// RequestAttack: bật khi player vào AttackRangeArea.
    /// - Set bởi: AttackRangeSensor.InRangeChanged
    /// - Đọc bởi: UtilityBrain.ScoreAttack (normal mode)
    /// </summary>
    public bool RequestAttack;

    /// <summary>
    /// Yêu cầu ReturnHome (dự phòng/mở rộng).
    /// Hiện tại ReturnHome chủ yếu do UtilityBrain tính dựa theo khoảng cách home/leash.
    /// </summary>
    public bool RequestReturnHome;

    // =========================================================
    // 7) Leash / Anti-Flip-Flop (giới hạn đuổi & chống lạm dụng)
    // =========================================================

    /// <summary>
    /// LeashBroken: cờ "đứt leash" khi enemy đã đuổi quá xa home.
    /// - Set/clear bởi: UtilityBrain (hysteresis Enter/Exit)
    /// - Đọc bởi: UtilityBrain (chặn Chase/Investigate, ép ReturnHome)
    /// </summary>
    public bool LeashBroken = false;

    /// <summary>
    /// Ngưỡng bật leash (Enter). distHome >= Enter -> LeashBroken=true.
    /// </summary>
    public float LeashEnterDist = 700;

    /// <summary>
    /// Ngưỡng tắt leash (Exit). distHome <= Exit -> LeashBroken=false.
    /// Lưu ý: Exit < Enter để tạo hysteresis, tránh rung qua lại.
    /// </summary>
    public float LeashExitDist = 250;

    /// <summary>
    /// RetaliateTimer: cửa sổ phản kích ngắn khi đang LeashBroken (đang quay về).
    /// - Set bởi: EnemyBrain.OnDamaged (khi bị đánh lúc đang ReturnHome/leash)
    /// - Giảm dần bởi: EnemyMemory.Tick
    /// - Đọc bởi: UtilityBrain.ScoreAttack (leash mode)
    /// </summary>
    public double RetaliateTimer = 0;
}
