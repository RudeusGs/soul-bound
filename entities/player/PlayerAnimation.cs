using Godot;

public partial class PlayerAnimation : Node
{
    private AnimatedSprite2D _sprite;
    private string _lastDir = "down";

    public override void _Ready()
    {
        _sprite = GetParent().GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    }

    public void UpdateAnimation(Vector2 velocity, int level)
    {
        if (_sprite == null) return;

        if (velocity == Vector2.Zero)
        {
            _sprite.Play($"lv{level}_idle_{_lastDir}");
            return;
        }

        string dir = GetDirection(velocity);
        _lastDir = dir;

        _sprite.Play($"lv{level}_walk_{dir}");
    }

    private string GetDirection(Vector2 v)
    {
        if (Mathf.Abs(v.X) > Mathf.Abs(v.Y))
            return v.X > 0 ? "right" : "left";
        else
            return v.Y > 0 ? "down" : "up";
    }
}
