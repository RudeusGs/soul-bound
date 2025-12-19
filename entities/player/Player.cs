using Godot;

public partial class Player : CharacterBody2D
{
    [Export] public float BaseMoveSpeed = 150f;
    [Export] public int Level = 1;

    public PlayerController Controller { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerAnimation Animation { get; private set; }
    public PlayerAttack Attack { get; private set; }

    // Khởi tạo và liên kết các module của Player
    public override void _Ready()
    {
        AddToGroup("player");
        Controller = GetNode<PlayerController>("PlayerController");
        Movement = GetNode<PlayerMovement>("PlayerMovement");
        Animation = GetNode<PlayerAnimation>("PlayerAnimation");
        Attack = GetNode<PlayerAttack>("PlayerAttack");

        Movement.Setup(this);
        Attack.Setup(this, Movement, Animation);
    }

    // Điều phối luồng xử lý mỗi frame vật lý của Player
    public override void _PhysicsProcess(double delta)
    {
        Controller.Tick();

        Attack.Tick(Controller.IsAttacking, delta);

        if (!Attack.IsAttacking)
        {
            Movement.Tick(
                Controller.MoveInput,
                Controller.IsRunning,
                delta
            );

            Animation.UpdateAnimation(
                Velocity,
                Level,
                Controller.IsRunning
            );
        }
    }
}
