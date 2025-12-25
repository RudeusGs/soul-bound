using Godot;

public partial class EnemyHitReceiver : Node
{
    [ExportGroup("Links")]
    [Export] public NodePath SpritePath = "../AnimatedSprite2D";

    [ExportGroup("Flash")]
    [Export] public float FlashDuration = 0.08f;
    [Export] public float FlashIntensity = 1.0f;

    private CanvasItem _sprite;
    private Enemy _enemy;

    public override void _Ready()
    {
        _enemy = GetParent() as Enemy;
        _sprite = GetNodeOrNull<CanvasItem>(SpritePath);

        if (_sprite == null)
            GD.PrintErr("[EnemyHitReceiver] Missing AnimatedSprite2D. Check SpritePath.");
    }

    public void OnHit(Node2D attacker)
    {
        if (_sprite != null)
            CombatVfx.HitFlash(_sprite, FlashDuration, FlashIntensity);

        _enemy?.OnDamaged(attacker);
    }
}