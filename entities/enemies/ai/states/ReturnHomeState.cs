using Godot;

/// <summary>
/// ReturnHomeState
///
/// Trạng thái dùng khi Enemy:
/// - Đã mất dấu hoàn toàn người chơi
/// - Mức nghi ngờ (Suspicion) thấp
/// - Hoặc đã đi quá xa vị trí ban đầu (home)
///
/// Hành vi:
/// - Enemy di chuyển về vị trí _home với tốc độ đi bộ
/// - Khi đến gần _home trong phạm vi _stopDist thì dừng lại
/// - Không tấn công, không chase, không investigate
///
/// State này giúp AI:
/// - Không lang thang vô hạn sau khi chase
/// - Trở lại trạng thái "bình thường" (Patrol / Idle)
/// - Hành vi giống con người: mất dấu thì quay về chỗ cũ
///
/// Lưu ý:
/// - ReturnHomeState KHÔNG quyết định khi nào được kích hoạt
/// - EnemyBrain / UtilityBrain chịu trách nhiệm chuyển sang state này
/// - State này chỉ xử lý movement về home
/// </summary>
public sealed class ReturnHomeState : IEnemyState
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
    /// Vị trí "home" – thường là vị trí spawn ban đầu của Enemy.
    /// </summary>
    private readonly Vector2 _home;

    /// <summary>
    /// Khoảng cách tối thiểu để coi như đã về tới home.
    /// Khi nhỏ hơn giá trị này thì Enemy sẽ dừng lại.
    /// </summary>
    private readonly float _stopDist;

    /// <summary>
    /// Khởi tạo ReturnHomeState.
    ///
    /// enemy    : Enemy owner
    /// move     : Module di chuyển
    /// home     : Vị trí home (spawn position)
    /// stopDist : Khoảng cách dừng khi về tới home
    /// </summary>
    public ReturnHomeState(Enemy enemy, EnemyMovement move, Vector2 home, float stopDist = 8f)
    {
        _enemy = enemy;
        _move = move;
        _home = home;
        _stopDist = stopDist;
    }

    /// <summary>
    /// Được gọi khi state này bắt đầu.
    /// Hiện tại không cần xử lý gì thêm.
    /// </summary>
    public void Enter() 
    {
        _enemy.BB.IsChasing = false;
        _enemy.BB.IsAttacking = false;
    }

    /// <summary>
    /// Được gọi khi rời khỏi state này.
    /// Hiện tại không cần cleanup gì.
    /// </summary>
    public void Exit() { }

    /// <summary>
    /// Được gọi mỗi frame khi state đang active.
    /// Thực hiện di chuyển Enemy về vị trí home.
    /// </summary>
    public void Tick(double delta)
    {
        var dist = _enemy.GlobalPosition.DistanceTo(_home);
        if (dist <= _stopDist)
        {
            _move.Stop();
            return;
        }
        var dir = Steering.Seek(_enemy.GlobalPosition, _home);
        _move.SetDesiredVelocity(dir * _enemy.WalkSpeed);
    }
}
