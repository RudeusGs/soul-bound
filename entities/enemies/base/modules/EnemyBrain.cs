using Godot;
using System;

public partial class EnemyBrain : Node
{
    private Enemy _enemy;
    private EnemyMovement _movement;
    private EnemyCombat _combat;
    private EnemyAnimation _anim;
    private EnemyBlackboard _bb;

    private EnemyMemory _memory;
    private UtilityBrain _utility;
    private StateMachine _sm;
    private double _stateLockTimer = 0;
    private Vector2 _homePos;

    [ExportGroup("AI/Memory")]
    [Export] public MemoryConfig MemoryCfg;

    [ExportGroup("AI/Links")]
    [Export] public NodePath VisionSensorPath = "../VisionArea";
    [Export] public NodePath AttackRangeSensorPath = "../AttackRangeArea";

    private AttackRangeSensor _attackRange;
    private VisionSensor _vision;

    public void Setup(Enemy enemy, EnemyMovement movement, EnemyCombat combat, EnemyAnimation anim, EnemyBlackboard bb)
    {
        _enemy = enemy;
        _movement = movement;
        _combat = combat;
        _anim = anim;
        _bb = bb;

        _homePos = _enemy.GlobalPosition;

        if (MemoryCfg == null)
            MemoryCfg = new MemoryConfig();

        _memory = new EnemyMemory(_bb, MemoryCfg);
        _utility = new UtilityBrain(_enemy, _bb, _combat, _homePos);

        SetupPerception();
        SetupStates();
    }

    private void SetupPerception()
    {
        _vision = GetNodeOrNull<VisionSensor>(VisionSensorPath);
        if (_vision != null)
        {
            _vision.Perceived += e =>
            {
                if (e.IsVisual)
                {
                    if (e.Strength <= 0f)
                    {
                        if (_bb.Target == e.Actor)
                        {
                            if (_bb.IsAttacking || _bb.RequestAttack)
                                return;
                            _bb.LastKnownTargetPos = e.Position;
                            _bb.HasLastKnownPos = true;
                            _bb.Target = null;               
                            _bb.LoseSightTimer = 2.0f;     
                            _bb.TimeSinceLastSeen = 0;
                        }
                    }
                    else
                    {
                        _memory.OnSee(e.Actor, e.Position, e.Strength);
                    }
                }
            };

        }
        else
        {
            GD.PrintErr("[EnemyBrain] Missing VisionSensor. Attach VisionSensor.cs to VisionArea and set VisionSensorPath.");
        }
        _attackRange = GetNodeOrNull<AttackRangeSensor>(AttackRangeSensorPath);
        if (_attackRange != null)
        {
            _attackRange.InRangeChanged += (actor, inRange) =>
            {
                if (inRange)
                {
                    if (_bb.Target == null || !_bb.Target.IsInsideTree())
                        _bb.Target = actor;

                    _bb.RequestAttack = true;
                }
                else
                {
                    _bb.RequestAttack = false;
                }
            };
        }
        else
        {
            GD.PrintErr("[EnemyBrain] Missing AttackRangeSensor. Attach AttackRangeSensor.cs to AttackRangeArea and set AttackRangeSensorPath.");
        }
    }

    private void SetupStates()
    {
        _sm = new StateMachine();

        _sm.Add(new PatrolState(_enemy, _movement, _homePos, radius: 200f, stopDist: 6f));
        _sm.Add(new InvestigateState(_enemy, _movement, _bb));
        _sm.Add(new ChaseState(_enemy, _movement, _bb, stopEnter: 10f, stopExit: 16f, leadTime: 0.25f));
        _sm.Add(new AttackState(_movement, _combat, _bb));
        _sm.Add(new ReturnHomeState(_enemy, _movement, _homePos));

        _sm.Change<PatrolState>();
    }

    public void Tick(double delta)
    {
        _memory.Tick(delta);

        if (_stateLockTimer > 0)
            _stateLockTimer = Math.Max(0, _stateLockTimer - delta);

        var action = _utility.Decide();

        // 1) Urgent actions phải chuyển ngay (không bị lock)
        bool urgent =
            action == UtilityAction.Attack ||
            action == UtilityAction.Chase ||
            (_bb.LeashBroken && action == UtilityAction.ReturnHome);


        // 2) Nếu state hiện tại "kẹt" / không còn hợp lệ -> cho phép thoát ngay
        bool forceExitStuck =
            (_sm.Current is ChaseState && (_bb.Target == null || !_bb.Target.IsInsideTree()) && _bb.LoseSightTimer <= 0 && !_bb.HasLastKnownPos)
            || (_sm.Current is InvestigateState && _bb.Target != null && _bb.Target.IsInsideTree());

        if (urgent || forceExitStuck || _stateLockTimer <= 0)
        {
            if (SwitchIfNeeded(action))
                _stateLockTimer = 0.20; // chống flip-flop cho các state không urgent
        }

        _sm.Tick(delta);
    }

    private bool SwitchIfNeeded(UtilityAction a)
    {
        switch (a)
        {
            case UtilityAction.Attack:
                return _sm.Change<AttackState>();
            case UtilityAction.Chase:
                return _sm.Change<ChaseState>();
            case UtilityAction.Investigate:
                return _sm.Change<InvestigateState>();
            case UtilityAction.ReturnHome:
                _bb.RequestAttack = false;
                _bb.IsAttacking = false;
                _bb.Target = null;
                _bb.HasLastKnownPos = false;
                _bb.LoseSightTimer = 0;
                return _sm.Change<ReturnHomeState>();

            case UtilityAction.Patrol:
                return _sm.Change<PatrolState>();
            default:
                return _sm.Change<PatrolState>();
        }
    }



    public void OnDamaged(Node2D attacker)
    {
        if (attacker == null || _bb.IsDead)
            return;

        // Nếu leash đứt: chỉ phản kích ngắn, không chase
        if (_bb.LeashBroken)
        {
            _bb.Target = attacker;
            _bb.RetaliateTimer = 1.0;

            // RequestAttack chỉ true khi thật sự đang trong tầm đánh (tránh giữ target “ảo”)
            _bb.RequestAttack = _combat.IsInRange(attacker);
        }

        // 1) Tăng mạnh nghi ngờ & cảnh giác
        _bb.Suspicion = Mathf.Clamp(_bb.Suspicion + 0.9f, 0f, 1f);
        _bb.Alertness = Mathf.Clamp(_bb.Alertness + 0.7f, 0f, 1f);
        _bb.DamageAwareness = Mathf.Clamp(_bb.DamageAwareness + 1.0f, 0f, 1f);
        // 2) Nếu thấy được attacker → lock target
        if (attacker.IsInsideTree())
        {
            _bb.Target = attacker;
            _bb.LastKnownTargetPos = attacker.GlobalPosition;
            _bb.HasLastKnownPos = true;
            _bb.TimeSinceLastSeen = 0;
        }
        else
        {
            // 3) Không thấy rõ → chỉ biết hướng/điểm bị đánh
            _bb.LastKnownTargetPos = _enemy.GlobalPosition;
            _bb.HasLastKnownPos = true;
            _bb.TimeSinceLastHeard = 0;
        }
    }

}
