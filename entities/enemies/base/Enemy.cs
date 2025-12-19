using Godot;

public partial class Enemy : CharacterBody2D
{
    [Export] public float WalkSpeed = 70f;
    [Export] public float RunSpeed = 100f;


    public EnemyMovement Movement { get; private set; }
    public EnemyCombat Combat { get; private set; }
    public EnemyAnimation Anim { get; private set; }
    public EnemyBrain Brain { get; private set; }
    public EnemyBlackboard BB { get; private set; }

    public override void _Ready()
    {
        // Blackboard là dữ liệu dùng chung cho mọi module
        BB = new EnemyBlackboard();

        Movement = GetNode<EnemyMovement>("EnemyMovement");
        Combat = GetNode<EnemyCombat>("EnemyCombat");
        Anim = GetNode<EnemyAnimation>("EnemyAnimation");
        Brain = GetNode<EnemyBrain>("EnemyBrain");

        Movement.Setup(this);
        Combat.Setup(this);
        Anim.Setup(this);
        Brain.Setup(this, Movement, Combat, Anim, BB);
    }

    public override void _PhysicsProcess(double delta)
    {
        // 1) Brain quyết định intent (muốn đi đâu, muốn đánh không…)
        Brain.Tick(delta);

        // 2) Combat xử lý cooldown/attack (Brain có thể yêu cầu Combat)
        Combat.Tick(delta);

        // 3) Movement áp velocity và MoveAndSlide
        Movement.Tick(delta);

        // 4) Animation đọc state/velocity và phát anim đúng
        Anim.Tick(delta);
    }

    // Hook cho hệ damage gọi vào đây
    public void OnDamaged(Node2D attacker)
    {
        BB.LastAttacker = attacker;
        BB.RememberAggro(attacker);
        Brain.OnDamaged(attacker);
    }
}
