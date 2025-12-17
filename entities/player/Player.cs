using Godot;

public partial class Player : CharacterBody2D
{
    [Export] public float BaseMoveSpeed = 120f;
    [Export] public int Level = 1;

    public PlayerController Controller { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerAnimation Animation { get; private set; }

    public override void _Ready()
    {
        Controller = GetNode<PlayerController>("PlayerController");
        Movement = GetNode<PlayerMovement>("PlayerMovement");
        Animation = GetNode<PlayerAnimation>("PlayerAnimation");

        Movement.Setup(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        Movement.Tick(Controller.MoveInput, delta);
        Animation.UpdateAnimation(Velocity, Level);
    }
}
