using Godot;

public partial class EnemyBrain : Node
{
    private Enemy _enemy;
    private EnemyMovement _movement;
    private EnemyCombat _combat;
    private EnemyAnimation _anim;
    private EnemyBlackboard _bb;

    [ExportGroup("Patrol")]
    [Export] public bool EnablePatrol = true;
    [Export] public float PatrolRadius = 200f;
    [Export] public float StopDistance = 6f;
    [Export] public float IdleMinTime = 1.0f;
    [Export] public float IdleMaxTime = 3.0f;

    private Vector2 _homePos;
    private Vector2 _patrolTarget;
    private bool _hasTarget;
    private double _idleTimer;

    public void Setup(Enemy enemy, EnemyMovement movement, EnemyCombat combat, EnemyAnimation anim, EnemyBlackboard bb)
    {
        _enemy = enemy;
        _movement = movement;
        _combat = combat;
        _anim = anim;
        _bb = bb;
        _homePos = _enemy.GlobalPosition;
    }

    public void Tick(double delta)
    {
        _bb.IsAttacking = false;

        if (!EnablePatrol)
        {
            _movement.Stop();
            return;
        }

        if (_idleTimer > 0)
        {
            _idleTimer -= delta;
            _movement.Stop();
            return;
        }

        if (!_hasTarget)
        {
            _patrolTarget = PickRandomPointInRadius(_homePos, PatrolRadius);
            _hasTarget = true;
        }

        var dist = _enemy.GlobalPosition.DistanceTo(_patrolTarget);
        if (dist <= StopDistance)
        {
            _hasTarget = false;
            _idleTimer = GD.RandRange(IdleMinTime, IdleMaxTime);
            _movement.Stop();
            return;
        }

        var dir = (_patrolTarget - _enemy.GlobalPosition).Normalized();
        _movement.SetDesiredVelocity(dir * _enemy.MoveSpeed);
    }

    public void OnDamaged(Node2D attacker)
    {
        // Tạm thời bỏ qua. Sau này neutral/hostile/boss sẽ xử lý ở FSM.
        // Nếu muốn: bị đánh thì dừng tuần tra, chuyển sang chase.
    }

    private Vector2 PickRandomPointInRadius(Vector2 center, float radius)
    {
        var angle = (float)GD.RandRange(0, Mathf.Tau);
        var r = radius * Mathf.Sqrt((float)GD.Randf());
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
    }
}
