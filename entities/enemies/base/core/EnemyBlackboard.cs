using Godot;

public sealed class EnemyBlackboard
{
    public Node2D Target;            // target hiện tại (nếu đã “lock”)
    public FacingDir Facing = FacingDir.Down;
    public float DamageAwareness;
    public double LoseSightTimer;
    // “Bằng chứng” / thông tin nhận thức
    public Vector2 LastKnownTargetPos;
    public bool HasLastKnownPos;

    public float Suspicion;          // 0..1 (nghi ngờ)
    public float Alertness;          // 0..1 (cảnh giác tổng quát)

    public bool IsChasing;
    public bool IsAttacking;
    public bool IsDead;

    // Timestamps/Timers (AI “con người” cần decay theo thời gian)
    public double TimeSinceLastSeen;
    public double TimeSinceLastHeard;

    // “hành vi”
    public bool RequestInvestigate;
    public bool RequestChase;
    public bool RequestAttack;
    public bool RequestReturnHome;
}
