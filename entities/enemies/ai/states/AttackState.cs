using Godot;

/// <summary>
/// AttackState
///
/// Trạng thái tấn công trực tiếp khi Enemy:
/// - Đã tiếp cận target trong phạm vi tấn công
/// - EnemyBrain / UtilityBrain quyết định rằng "đã đến lúc đánh"
///
/// Hành vi:
/// - Enemy dừng di chuyển hoàn toàn
/// - Gọi EnemyCombat.DoAttack() để xử lý sát thương / cooldown
/// - Giữ trạng thái IsAttacking = true để animation phát đúng
///
/// Mục đích:
/// - Tách rõ logic "đánh" ra khỏi ChaseState
/// - Giữ AttackState đơn giản, dễ mở rộng (combo, charge, skill…)
/// - Tránh việc vừa chạy vừa đánh gây rối animation
///
/// Lưu ý quan trọng:
/// - AttackState KHÔNG kiểm tra khoảng cách tấn công
///   → UtilityBrain / EnemyCombat chịu trách nhiệm điều đó
/// - AttackState KHÔNG tự chuyển state
///   → EnemyBrain quyết định khi nào quay lại Chase hoặc state khác
/// </summary>
public sealed class AttackState : IEnemyState
{
    /// <summary>
    /// Module movement dùng để dừng Enemy khi tấn công.
    /// </summary>
    private readonly EnemyMovement _move;

    /// <summary>
    /// Module combat xử lý logic tấn công (cooldown, damage).
    /// </summary>
    private readonly EnemyCombat _combat;

    /// <summary>
    /// Blackboard – nơi lưu target và các flag hành vi.
    /// </summary>
    private readonly EnemyBlackboard _bb;

    /// <summary>
    /// Khởi tạo AttackState.
    ///
    /// move   : Module di chuyển
    /// combat : Module combat
    /// bb     : Blackboard
    /// </summary>
    public AttackState(EnemyMovement move, EnemyCombat combat, EnemyBlackboard bb)
    {
        _move = move;
        _combat = combat;
        _bb = bb;
    }

    /// <summary>
    /// Được gọi khi bắt đầu tấn công.
    /// - Set IsAttacking để animation chuyển sang attack
    /// - Dừng toàn bộ movement
    /// </summary>
    public void Enter()
    {
        _bb.IsChasing = false;
        _bb.IsAttacking = true;
        _move.Stop();
    }

    /// <summary>
    /// Được gọi khi rời khỏi trạng thái tấn công.
    /// - Reset flag IsAttacking
    /// </summary>
    public void Exit()
    {
        _bb.IsAttacking = false;
    }

    /// <summary>
    /// Được gọi mỗi frame khi state đang active.
    /// - Nếu target không còn hợp lệ → không làm gì
    /// - Nếu target hợp lệ → gọi EnemyCombat.DoAttack()
    ///
    /// Cooldown và logic sát thương được xử lý bên trong EnemyCombat.
    /// </summary>
    public void Tick(double delta)
    {
        if (_bb.Target == null || !_bb.Target.IsInsideTree())
            return;

        _move.Stop();
        _combat.DoAttack(_bb.Target);
    }
}
