using Godot;

/// <summary>
/// PlayerHitReceiver
/// - Nhận hit (enemy gọi vào)
/// - Chạy flash bằng overlay additive (đẹp, không làm tối sprite gốc)
/// - Có invuln ngắn để tránh spam hit/flash
/// </summary>
public partial class PlayerHitReceiver : Node
{
    [ExportGroup("Links")]
    [Export] public NodePath BaseSpritePath = "../PlayerAnimated";
    [Export] public NodePath OverlaySpritePath = "../PlayerAnimated/FlashOverlay";

    [ExportGroup("Flash Look")]
    [Export] public Color FlashColor = new Color(1, 1, 1, 1);
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float PeakAlpha = 0.45f;
    [Export] public float InTime = 0.04f;
    [Export] public float OutTime = 0.18f;
    [Export] public int PulseCount = 1;
    [Export] public float PulseGap = 0.03f;
    [Export(PropertyHint.Range, "0,1,0.01")]
    public float NextPulseScale = 0.6f;

    [ExportGroup("Rules")]
    [Export] public float InvulnTime = 0.20f;

    private AnimatedSprite2D _base;
    private AnimatedSprite2D _overlay;

    private CanvasItemMaterial _addMat;
    private Tween _tween;

    private double _invulnTimer = 0;

    public override void _Ready()
    {
        _base = GetNodeOrNull<AnimatedSprite2D>(BaseSpritePath);
        _overlay = GetNodeOrNull<AnimatedSprite2D>(OverlaySpritePath);

        if (_base == null)
        {
            GD.PrintErr("[PlayerHitReceiver] Missing PlayerAnimated. Check BaseSpritePath.");
            return;
        }
        if (_overlay == null)
        {
            GD.PrintErr("[PlayerHitReceiver] Missing FlashOverlay. Create PlayerAnimated/FlashOverlay and set OverlaySpritePath.");
            return;
        }
        _overlay.SpriteFrames = _base.SpriteFrames;
        _overlay.SpeedScale = 0f;
        _overlay.Visible = false;
        _addMat = new CanvasItemMaterial();
        _addMat.BlendMode = CanvasItemMaterial.BlendModeEnum.Add;
        _overlay.Material = _addMat;
        var c0 = FlashColor; c0.A = 0f;
        _overlay.Modulate = c0;

        SetProcess(false);
        SetPhysicsProcess(false);
    }

    /// <summary>Enemy gọi vào đây khi hitbox trúng player</summary>
    public void OnHit(Node2D attacker)
    {
        if (_invulnTimer > 0) return;

        _invulnTimer = Mathf.Max(0.0f, InvulnTime);
        if (_invulnTimer > 0) SetPhysicsProcess(true);

        StartFlash();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_invulnTimer <= 0)
        {
            SetPhysicsProcess(false);
            return;
        }

        _invulnTimer -= delta;
        if (_invulnTimer <= 0)
            SetPhysicsProcess(false);
    }

    public override void _Process(double delta)
    {
        if (_overlay == null || !_overlay.Visible)
        {
            SetProcess(false);
            return;
        }

        SyncOverlay();
    }

    private void StartFlash()
    {
        if (_base == null || _overlay == null) return;

        _tween?.Kill();

        _overlay.Visible = true;
        SetProcess(true);

        SyncOverlay();

        int pulses = Mathf.Max(1, PulseCount);
        float tin = Mathf.Max(0.01f, InTime);
        float tout = Mathf.Max(0.01f, OutTime);

        var c0 = FlashColor; c0.A = 0f;

        _overlay.Modulate = c0;

        var t = CreateTween();
        _tween = t;

        float alpha = Mathf.Clamp(PeakAlpha, 0f, 1f);
        float scale = Mathf.Clamp(NextPulseScale, 0f, 1f);

        for (int i = 0; i < pulses; i++)
        {
            float a = alpha * Mathf.Pow(scale, i);
            var c1 = FlashColor; c1.A = a;

            t.TweenProperty(_overlay, "modulate", c1, tin)
             .SetTrans(Tween.TransitionType.Sine)
             .SetEase(Tween.EaseType.Out);

            t.TweenProperty(_overlay, "modulate", c0, tout)
             .SetTrans(Tween.TransitionType.Sine)
             .SetEase(Tween.EaseType.Out);

            if (i < pulses - 1)
                t.TweenInterval(Mathf.Max(0f, PulseGap));
        }

        t.Finished += () =>
        {
            if (_overlay == null || !_overlay.IsInsideTree()) return;
            _overlay.Modulate = c0;
            _overlay.Visible = false;
        };
    }

    private void SyncOverlay()
    {
        if (_base == null || _overlay == null) return;

        if (_overlay.SpriteFrames != _base.SpriteFrames)
            _overlay.SpriteFrames = _base.SpriteFrames;

        _overlay.Animation = _base.Animation;
        _overlay.Frame = _base.Frame;
        _overlay.FlipH = _base.FlipH;
        _overlay.FlipV = _base.FlipV;
        _overlay.Offset = _base.Offset;
        _overlay.Centered = _base.Centered;
    }
}
