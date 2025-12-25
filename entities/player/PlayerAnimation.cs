using Godot;

public partial class PlayerAnimation : Node
{
    private AnimatedSprite2D _sprite;
    private string _lastDir = "down";
    public string LastDirection => _lastDir;
    // Lấy AnimatedSprite2D của Player
    public override void _Ready()
    {
        _sprite = GetParent().GetNode<AnimatedSprite2D>("PlayerAnimated");
    }

    // Cập nhật animation idle / walk / run
    public void UpdateAnimation(Vector2 velocity, int level, bool isRunning)
    {
        if (_sprite == null) return;

        if (velocity == Vector2.Zero)
        {
            _sprite.Play($"lv{level}_idle_{_lastDir}");
            return;
        }

        string state = isRunning ? "run" : "walk";
        string dir = GetDirection(velocity);
        _lastDir = dir;

        _sprite.Play($"lv{level}_{state}_{dir}");
    }

    // Xác định hướng di chuyển từ velocity
    private string GetDirection(Vector2 v)
    {
        if (Mathf.Abs(v.X) > Mathf.Abs(v.Y))
            return v.X > 0 ? "right" : "left";
        else
            return v.Y > 0 ? "down" : "up";
    }

    // Chạy animation tấn công theo hướng
    public void PlayAttack(Vector2 velocity, int level)
    {
        string dir = velocity == Vector2.Zero ? _lastDir : GetDirection(velocity);
        _lastDir = dir;

        _sprite.Play($"lv{level}_attack_{dir}");
    }
}
