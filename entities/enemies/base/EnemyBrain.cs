using Godot;

public partial class EnemyBrain : Node
{
    private Enemy _enemy;
    private EnemyMovement _movement;
    private EnemyCombat _combat;
    private EnemyAnimation _anim;
    private EnemyBlackboard _bb;

    [ExportGroup("Vision")]
    [Export] public NodePath VisionAreaPath = "../VisionArea";
    [Export] public bool LoseTargetWhenExit = true;

    [ExportGroup("Chase")]
    [Export] public float ChaseStopDistance = 10f;

    [ExportGroup("Patrol")]
    [Export] public bool EnablePatrol = true;
    [Export] public float PatrolRadius = 200f;
    [Export] public float StopDistance = 6f;
    [Export] public float IdleMinTime = 1.0f;
    [Export] public float IdleMaxTime = 3.0f;

    private Area2D _visionArea;

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

        _visionArea = GetNodeOrNull<Area2D>(VisionAreaPath);
        if (_visionArea != null)
        {
            _visionArea.BodyEntered += OnVisionBodyEntered;
            _visionArea.BodyExited += OnVisionBodyExited;
        }
        else
        {
            GD.PrintErr("[EnemyBrain] Missing VisionArea node.");
        }
    }

    public void Tick(double delta)
    {
        if (_bb.Target != null && _bb.Target.IsInsideTree())
        {
            _bb.IsChasing = true;
            _bb.IsAttacking = false;
            var toTarget = _bb.Target.GlobalPosition - _enemy.GlobalPosition;
            if (toTarget.Length() <= ChaseStopDistance)
            {
                _movement.Stop();
            }
            else
            {
                var dir = toTarget.Normalized();
                _movement.SetDesiredVelocity(dir * _enemy.RunSpeed);

            }

            return;
        }

        _bb.IsChasing = false;
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
        var dir2 = (_patrolTarget - _enemy.GlobalPosition).Normalized();
        _movement.SetDesiredVelocity(dir2 * _enemy.WalkSpeed);

    }

    private void OnVisionBodyEntered(Node body)
    {
        if (body is Node2D n2d && body.IsInGroup("player"))
        {
            _bb.Target = n2d;
        }
    }

    private void OnVisionBodyExited(Node body)
    {
        if (!LoseTargetWhenExit) return;

        if (body == _bb.Target)
        {
            _bb.Target = null;
        }
    }

    public void OnDamaged(Node2D attacker)
    {
        if (attacker != null && attacker.IsInGroup("player"))
            _bb.Target = attacker;
    }

    private Vector2 PickRandomPointInRadius(Vector2 center, float radius)
    {
        var angle = (float)GD.RandRange(0, Mathf.Tau);
        var r = radius * Mathf.Sqrt((float)GD.Randf());
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
    }
}
