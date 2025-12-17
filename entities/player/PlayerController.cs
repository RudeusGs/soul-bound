using Godot;

public partial class PlayerController : Node
{
    public Vector2 MoveInput { get; private set; }

    public override void _Process(double delta)
    {
        MoveInput = Input.GetVector(
            "ui_left",
            "ui_right",
            "ui_up",
            "ui_down"
        );
    }
}
