using Godot;

public partial class PlayerController : Node
{
    public Vector2 MoveInput { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsAttacking { get; private set; }

    // Đọc input từ người chơi và lưu trạng thái điều khiển
    public void Tick()
    {
        MoveInput = Input.GetVector(
            "ui_left",
            "ui_right",
            "ui_up",
            "ui_down"
        );

        IsRunning = Input.IsActionPressed("run");
        IsAttacking = Input.IsActionPressed("attack");
    }
}
