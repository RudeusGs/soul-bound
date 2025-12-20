using Godot;

public partial class EnemyAnimation : Node
{
    private Enemy _enemy;
    private AnimatedSprite2D _sprite;
    private string _current = "";

    [Export] public string AnimPrefix = "smileboss1"; // đổi per enemy
    [Export] public bool HasDeadAnim = false;
    [Export] public bool HasAttackAnim = false;

    public void Setup(Enemy enemy)
    {
        _enemy = enemy;
        _sprite = _enemy.GetNode<AnimatedSprite2D>("AnimatedSprite2D");
    }

    public void Tick(double delta)
    {
        var bb = _enemy.BB;
        var v = _enemy.Velocity;

        if (bb.IsDead && HasDeadAnim)
        {
            Play($"{AnimPrefix}_dead");
            return;
        }

        if (bb.IsAttacking && HasAttackAnim)
        {
            Play($"{AnimPrefix}_attack");
            return;
        }

        if (v.Length() > 1f)
            bb.Facing = FacingFromVelocity(v);

        var suffix = FacingToSuffix(bb.Facing);

        if (v.Length() > 1f)
        {
            var moveAnim = bb.IsChasing ? "run" : "walk";
            Play($"{AnimPrefix}_{moveAnim}_{suffix}");
        }
        else
        {
            Play($"{AnimPrefix}_idle_{suffix}");
        }
    }

    private static FacingDir FacingFromVelocity(Vector2 v)
    {
        if (Mathf.Abs(v.X) > Mathf.Abs(v.Y))
            return v.X >= 0 ? FacingDir.Right : FacingDir.Left;
        else
            return v.Y >= 0 ? FacingDir.Down : FacingDir.Up;
    }

    private static string FacingToSuffix(FacingDir f) => f switch
    {
        FacingDir.Up => "up",
        FacingDir.Down => "down",
        FacingDir.Left => "left",
        FacingDir.Right => "right",
        _ => "down"
    };

    private void Play(string anim)
    {
        if (_current == anim) return;
        _current = anim;

        if (_sprite.SpriteFrames == null) return;
        if (!_sprite.SpriteFrames.HasAnimation(anim))
        {
            GD.Print($"[EnemyAnimation] Missing anim: {anim}");
            return;
        }

        _sprite.Play(anim);
    }
}
