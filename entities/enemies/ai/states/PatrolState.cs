using Godot;

/// <summary>
/// PatrolState
///
/// Trạng thái tuần tra mặc định của Enemy khi:
/// - Không có target
/// - Mức nghi ngờ (Suspicion) thấp
/// - Không cần investigate hay chase
///
/// Hành vi:
/// - Enemy chọn một điểm ngẫu nhiên quanh vị trí home trong bán kính _radius
/// - Di chuyển tới điểm đó với tốc độ WalkSpeed
/// - Khi tới nơi:
///     + Dừng lại một khoảng thời gian ngẫu nhiên (_idleTimer)
///     + Sau đó chọn điểm patrol mới
///
/// Mục đích:
/// - Tạo cảm giác Enemy "đi tuần" tự nhiên
/// - Tránh đứng yên một chỗ khi không có gì xảy ra
/// - Là trạng thái nền để chuyển sang Investigate / Chase khi có kích thích
///
/// Lưu ý:
/// - PatrolState KHÔNG quyết định khi nào được kích hoạt
/// - EnemyBrain / UtilityBrain chịu trách nhiệm chuyển sang state này
/// - State này chỉ xử lý movement tuần tra
/// </summary>
public sealed class PatrolState : IEnemyState
{
    /// <summary>
    /// Enemy sở hữu state này.
    /// Dùng để lấy vị trí hiện tại và tốc độ di chuyển.
    /// </summary>
    private readonly Enemy _enemy;

    /// <summary>
    /// Module movement dùng để điều khiển di chuyển.
    /// </summary>
    private readonly EnemyMovement _move;

    /// <summary>
    /// Vị trí home – thường là vị trí spawn ban đầu của Enemy.
    /// Các điểm patrol sẽ được chọn quanh vị trí này.
    /// </summary>
    private readonly Vector2 _home;

    /// <summary>
    /// Bán kính patrol quanh home.
    /// Enemy sẽ không đi ra ngoài phạm vi này khi tuần tra.
    /// </summary>
    private readonly float _radius;

    /// <summary>
    /// Khoảng cách để coi như đã tới điểm patrol.
    /// Khi nhỏ hơn giá trị này, Enemy sẽ dừng lại.
    /// </summary>
    private readonly float _stopDist;

    /// <summary>
    /// Điểm patrol hiện tại mà Enemy đang hướng tới.
    /// </summary>
    private Vector2 _target;

    /// <summary>
    /// Cho biết Enemy đã có target patrol hay chưa.
    /// </summary>
    private bool _hasTarget;

    /// <summary>
    /// Thời gian idle sau khi tới điểm patrol.
    /// Dùng để tạo khoảng dừng tự nhiên giữa các lần di chuyển.
    /// </summary>
    private double _idleTimer;

    /// <summary>
    /// Khởi tạo PatrolState.
    ///
    /// enemy    : Enemy owner
    /// move     : Module di chuyển
    /// home     : Vị trí home (spawn position)
    /// radius   : Bán kính tuần tra quanh home
    /// stopDist : Khoảng cách dừng khi tới điểm patrol
    /// </summary>
    public PatrolState(Enemy enemy, EnemyMovement move, Vector2 home, float radius = 200f, float stopDist = 6f)
    {
        _enemy = enemy;
        _move = move;
        _home = home;
        _radius = radius;
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
    /// Thực hiện logic tuần tra:
    /// - Idle nếu đang trong thời gian nghỉ
    /// - Chọn điểm patrol mới nếu chưa có
    /// - Di chuyển tới điểm patrol
    /// </summary>
    public void Tick(double delta)
    {
        // Nếu đang idle → dừng và đếm ngược
        if (_idleTimer > 0)
        {
            _idleTimer -= delta;
            _move.Stop();
            return;
        }

        // Nếu chưa có target patrol → chọn điểm mới quanh home
        if (!_hasTarget)
        {
            _target = RandomUtil.PointInRadius(_home, _radius);
            _hasTarget = true;
        }

        // Kiểm tra khoảng cách tới điểm patrol
        var dist = _enemy.GlobalPosition.DistanceTo(_target);
        if (dist <= _stopDist)
        {
            // Đã tới nơi → reset target và idle một chút
            _hasTarget = false;
            _idleTimer = GD.RandRange(1.0, 3.0);
            _move.Stop();
            return;
        }

        // Di chuyển về phía điểm patrol
        var dir = Steering.Seek(_enemy.GlobalPosition, _target);
        _move.SetDesiredVelocity(dir * _enemy.WalkSpeed);
    }
}
