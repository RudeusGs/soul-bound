using Godot;

/// <summary>
/// ChaseState
///
/// Trạng thái truy đuổi trực tiếp khi Enemy:
/// - Thấy target (Target != null)
/// - Hoặc vừa mới mất dấu nhưng còn trong "grace period" (LoseSightTimer > 0)
///
/// Hành vi:
/// - Enemy chạy theo target với tốc độ RunSpeed
/// - Sử dụng dự đoán vị trí (prediction) để bám theo mượt hơn
/// - Khi mất target:
///     + Nếu còn LoseSightTimer → chạy về LastKnownTargetPos
///     + Nếu hết LoseSightTimer → dừng chase
///
/// Mục đích:
/// - Tránh cảm giác AI "telepathic" (biết chính xác target mọi lúc)
/// - Tạo truy đuổi mượt, giống phản xạ con người
/// - Tránh rung animation bằng hysteresis (stopEnter / stopExit)
///
/// Lưu ý:
/// - ChaseState KHÔNG quyết định khi nào bắt đầu/kết thúc chase
/// - EnemyBrain / UtilityBrain chịu trách nhiệm chuyển state
/// - State này chỉ xử lý movement truy đuổi
/// </summary>
public sealed class ChaseState : IEnemyState
{
    /// <summary>
    /// Enemy sở hữu state này.
    /// </summary>
    private readonly Enemy _enemy;

    /// <summary>
    /// Module movement dùng để điều khiển di chuyển.
    /// </summary>
    private readonly EnemyMovement _move;

    /// <summary>
    /// Blackboard – nơi lưu thông tin target, trí nhớ và timer.
    /// </summary>
    private readonly EnemyBlackboard _bb;

    /// <summary>
    /// Khoảng cách để bắt đầu dừng lại khi tiếp cận target.
    /// </summary>
    private readonly float _stopEnter;

    /// <summary>
    /// Khoảng cách để bắt đầu chạy lại khi target ra xa.
    /// Dùng để tạo hysteresis (chống rung).
    /// </summary>
    private readonly float _stopExit;

    /// <summary>
    /// Thời gian dự đoán vị trí target trong tương lai.
    /// Giá trị nhỏ (0.2–0.3s) cho cảm giác bám mượt.
    /// </summary>
    private readonly float _leadTime;

    /// <summary>
    /// Vị trí target ở frame trước – dùng để tính vận tốc.
    /// </summary>
    private Vector2 _lastPos;

    /// <summary>
    /// Vận tốc ước lượng của target.
    /// </summary>
    private Vector2 _vel;

    /// <summary>
    /// Khởi tạo ChaseState.
    ///
    /// enemy     : Enemy owner
    /// move      : Module di chuyển
    /// bb        : Blackboard
    /// stopEnter : Khoảng cách dừng khi tới gần target
    /// stopExit  : Khoảng cách chạy lại khi target ra xa
    /// leadTime  : Thời gian dự đoán vị trí target
    /// </summary>
    public ChaseState(
        Enemy enemy,
        EnemyMovement move,
        EnemyBlackboard bb,
        float stopEnter = 10f,
        float stopExit = 16f,
        float leadTime = 0.25f)
    {
        _enemy = enemy;
        _move = move;
        _bb = bb;
        _stopEnter = stopEnter;
        _stopExit = stopExit;
        _leadTime = leadTime;
    }

    /// <summary>
    /// Được gọi khi bắt đầu chase.
    /// Set các flag liên quan đến hành vi.
    /// </summary>
    public void Enter()
    {
        _bb.IsChasing = true;
        _bb.IsAttacking = false;

        if (_bb.Target != null && _bb.Target.IsInsideTree())
            _lastPos = _bb.Target.GlobalPosition;
    }

    /// <summary>
    /// Được gọi khi rời khỏi chase.
    /// Hiện tại không cần cleanup gì thêm.
    /// </summary>
    public void Exit() { }

    /// <summary>
    /// Được gọi mỗi frame khi state đang active.
    /// Thực hiện logic truy đuổi:
    /// - Chase target nếu còn thấy
    /// - Chase theo trí nhớ nếu vừa mất dấu
    /// - Dừng chase nếu hết grace period
    /// </summary>
    public void Tick(double delta)
    {
        // Nếu không còn target
        if (_bb.Target == null || !_bb.Target.IsInsideTree())
        {
            // Nếu còn grace period → chạy về vị trí cuối cùng
            if (_bb.HasLastKnownPos && _bb.LoseSightTimer > 0)
            {
                var dir = Steering.Seek(_enemy.GlobalPosition, _bb.LastKnownTargetPos);
                _move.SetDesiredVelocity(dir * _enemy.RunSpeed);
                return;
            }

            // Hết grace → dừng chase
            _bb.IsChasing = false;
            _move.Stop();
            return;
        }

        // --- Target còn tồn tại ---

        // Tính vận tốc target dựa trên vị trí frame trước
        var cur = _bb.Target.GlobalPosition;
        _vel = (cur - _lastPos) / (float)Mathf.Max(0.0001, (float)delta);
        _lastPos = cur;

        // Cập nhật trí nhớ
        _bb.LastKnownTargetPos = cur;
        _bb.HasLastKnownPos = true;

        // Dự đoán vị trí target trong tương lai
        var predicted = cur + _vel * _leadTime;
        var dist = _enemy.GlobalPosition.DistanceTo(predicted);

        // Hysteresis:
        // - Vào gần thì dừng
        // - Ra xa mới chạy lại
        bool shouldStop = dist <= _stopEnter;
        bool shouldMove = dist >= _stopExit;

        if (shouldStop)
        {
            _move.Stop();
            return;
        }

        if (shouldMove || _enemy.Velocity.Length() > 0.1f)
        {
            var dir = Steering.Seek(_enemy.GlobalPosition, predicted);
            _move.SetDesiredVelocity(dir * _enemy.RunSpeed);
        }
    }
}
