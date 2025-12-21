using Godot;

public partial class EnemyAnimation : Node
{
    private Enemy _enemy;
    private AnimatedSprite2D _sprite;
    private string _current = "";
    [Export] public float MoveEnterSpeed = 12f;
    [Export] public float MoveExitSpeed = 6f;

    private bool _isMoving = false;
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

        if (HasAttackAnim && bb.IsAttacking)
        {
            UpdateFacingFromTargetIfAny(bb);

            var anim = $"{AnimPrefix}_attack_{FacingToSuffix(bb.Facing)}";
            Play(anim);
            return;
        }

        var v = _enemy.Velocity;
        float speed = v.Length();

        if (!_isMoving && speed >= MoveEnterSpeed)
            _isMoving = true;
        else if (_isMoving && speed <= MoveExitSpeed)
            _isMoving = false;

        if (_isMoving)
            bb.Facing = FacingFromVelocity(v);

        var suffix = FacingToSuffix(bb.Facing);

        if (_isMoving)
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

        _sprite.Play(anim);
    }
    private void UpdateFacingFromTargetIfAny(dynamic bb)
    {
        if (bb.Target == null) return;
        if (bb.Target is not Node2D target) return;

        Vector2 d = target.GlobalPosition - _enemy.GlobalPosition;
        if (d.LengthSquared() < 0.0001f) return;

        bb.Facing = FacingFromVelocity(d);
    }
}
