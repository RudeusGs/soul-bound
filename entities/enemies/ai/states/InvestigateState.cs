using Godot;

/// <summary>
/// InvestigateState
///
/// Trạng thái "điều tra" khi Enemy:
/// - Không còn thấy trực tiếp target
/// - Nhưng còn thông tin nghi ngờ (LastKnownTargetPos)
/// - Ví dụ: vừa mất dấu player, nghe tiếng động, hoặc bị đánh lén
///
/// Hành vi:
/// - Enemy di chuyển tới vị trí cuối cùng nghi ngờ (_bb.LastKnownTargetPos)
/// - Khi tới nơi thì dừng lại và "quan sát"
/// - Không tấn công, không chase trực tiếp
///
/// Mục đích:
/// - Tạo hành vi giống con người: mất dấu thì đi kiểm tra
/// - Tránh chuyển thẳng về Patrol ngay khi vừa mất target
/// - Cho Memory có thời gian decay Suspicion dần
///
/// Lưu ý:
/// - InvestigateState KHÔNG quyết định khi nào được kích hoạt
/// - EnemyBrain / UtilityBrain chịu trách nhiệm chuyển sang state này
/// - Nếu trong quá trình điều tra phát hiện lại target,
///   EnemyBrain sẽ chuyển sang ChaseState
/// </summary>
public sealed class InvestigateState : IEnemyState
{
    /// <summary>
    /// Enemy sở hữu state này.
    /// Dùng để lấy vị trí hiện tại và thông số tốc độ.
    /// </summary>
    private readonly Enemy _enemy;

    /// <summary>
    /// Module movement dùng để điều khiển di chuyển.
    /// </summary>
    private readonly EnemyMovement _move;

    /// <summary>
    /// Blackboard – nơi lưu thông tin nhận thức và trí nhớ của Enemy.
    /// </summary>
    private readonly EnemyBlackboard _bb;

    /// <summary>
    /// Khoảng cách để coi như đã tới vị trí cần điều tra.
    /// Khi nhỏ hơn giá trị này, Enemy sẽ dừng lại.
    /// </summary>
    [Export] private readonly float _stopDist = 8f;

    /// <summary>
    /// Khởi tạo InvestigateState.
    ///
    /// enemy : Enemy owner
    /// move  : Module di chuyển
    /// bb    : Blackboard dùng để đọc thông tin trí nhớ
    /// </summary>
    public InvestigateState(Enemy enemy, EnemyMovement move, EnemyBlackboard bb)
    {
        _enemy = enemy;
        _move = move;
        _bb = bb;
    }

    /// <summary>
    /// Được gọi khi state này bắt đầu.
    /// Reset các flag liên quan đến combat/chase.
    /// </summary>
    public void Enter()
    {
        _bb.IsChasing = false;
        _bb.IsAttacking = false;
    }

    /// <summary>
    /// Được gọi khi rời khỏi state này.
    /// Hiện tại không cần cleanup gì.
    /// </summary>
    public void Exit() { }

    /// <summary>
    /// Được gọi mỗi frame khi state đang active.
    /// Thực hiện hành vi điều tra:
    /// - Nếu thấy lại target → EnemyBrain sẽ chuyển state
    /// - Nếu không còn thông tin nghi ngờ → dừng lại
    /// - Nếu chưa tới vị trí cần điều tra → tiếp tục di chuyển
    /// </summary>
    public void Tick(double delta)
    {
        // Nếu phát hiện lại target trong lúc điều tra → dừng logic ở đây
        // EnemyBrain sẽ quyết định chuyển sang ChaseState
        if (_bb.Target != null && _bb.Target.IsInsideTree())
            return;

        // Không còn vị trí nghi ngờ → không làm gì
        if (!_bb.HasLastKnownPos)
        {
            _move.Stop();
            return;
        }

        // Khoảng cách tới vị trí cần điều tra
        var dist = _enemy.GlobalPosition.DistanceTo(_bb.LastKnownTargetPos);

        // Đã tới nơi → đứng lại và "quan sát"
        // Suspicion sẽ giảm dần trong EnemyMemory.Tick
        if (dist <= _stopDist)
        {
            _move.Stop();
            return;
        }

        // Di chuyển về phía vị trí nghi ngờ
        var dir = Steering.Seek(_enemy.GlobalPosition, _bb.LastKnownTargetPos);
        _move.SetDesiredVelocity(dir * _enemy.WalkSpeed);
    }
}
