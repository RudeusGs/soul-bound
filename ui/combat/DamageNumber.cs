using Godot;

/// <summary>
/// DamageNumber
/// Hiển thị damage bay lên + pop + arc + fade. Không chứa logic combat.
/// </summary>
public partial class DamageNumber : Node2D
{
    public enum DmgKind
    {
        Normal,     // đỏ
        Crit,       // vàng
        True,       // trắng
        Miss        // "ĐÁNH HỤT"
    }

    [ExportCategory("Motion")]
    [Export] public float FloatDistance = 28f;
    [Export] public float Duration = 0.75f;
    [Export] public float SideDrift = 18f;
    [Export] public float RandomOffsetX = 8f;
    [Export] public float RandomOffsetY = 6f;
    [Export] public float RandomRotationDeg = 6f;

    [ExportCategory("Pop / Scale")]
    [Export] public float BaseScale = 1.0f;
    [Export] public float NormalPop = 1.25f;
    [Export] public float CritPop = 1.45f;
    [Export] public float MissPop = 1.10f;

    [ExportCategory("Look")]
    [Export] public int NormalFontSize = 20;
    [Export] public int CritFontSize = 24;
    [Export] public int TrueFontSize = 20;
    [Export] public int MissFontSize = 18;

    [Export] public int OutlineSize = 6;
    [Export] public Color OutlineColor = new Color(0, 0, 0, 0.85f);

    private Label _label;
    private Tween _tween;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        var settings = _label.LabelSettings ?? new LabelSettings();
        settings.OutlineSize = OutlineSize;
        settings.OutlineColor = OutlineColor;
        _label.LabelSettings = settings;

        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.VerticalAlignment = VerticalAlignment.Center;

        _label.Scale = Vector2.One * BaseScale;
    }

    public void Play(int value, DmgKind kind)
    {
        _tween?.Kill();
        _label.Modulate = Colors.White;
        _label.Scale = Vector2.One * BaseScale;
        _label.Rotation = 0f;
        string text;
        Color color;
        int fontSize;
        float pop;

        switch (kind)
        {
            case DmgKind.Crit:
                text = value.ToString();
                color = new Color(1.0f, 0.86f, 0.25f);
                fontSize = CritFontSize;
                pop = CritPop;
                break;

            case DmgKind.True:
                text = value.ToString();
                color = Colors.White;
                fontSize = TrueFontSize;
                pop = NormalPop;
                break;

            case DmgKind.Miss:
                text = "Đánh hụt";
                color = new Color(0.85f, 0.85f, 0.85f);
                fontSize = MissFontSize;
                pop = MissPop;
                break;

            default: // Normal
                text = value.ToString();
                color = new Color(1.0f, 0.25f, 0.25f);
                fontSize = NormalFontSize;
                pop = NormalPop;
                break;
        }

        _label.Text = text;
        _label.Modulate = color;
        _label.AddThemeFontSizeOverride("font_size", fontSize);
        var startPos = GlobalPosition;
        startPos.X += (float)GD.RandRange(-RandomOffsetX, RandomOffsetX);
        startPos.Y += (float)GD.RandRange(-RandomOffsetY, RandomOffsetY);
        GlobalPosition = startPos;

        float rotRad = Mathf.DegToRad((float)GD.RandRange(-RandomRotationDeg, RandomRotationDeg));
        _label.Rotation = rotRad;
        float driftX = (float)GD.RandRange(-SideDrift, SideDrift);
        _tween = CreateTween();
        _tween.SetParallel(true);
        _tween.TweenProperty(
            this,
            "global_position",
            new Vector2(startPos.X + driftX, startPos.Y - FloatDistance),
            Duration
        ).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        var popTween = CreateTween();
        popTween.TweenProperty(_label, "scale", Vector2.One * (BaseScale * pop), Duration * 0.18f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Back);
        popTween.TweenProperty(_label, "scale", Vector2.One * BaseScale, Duration * 0.22f)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        float fadeStart = Duration * 0.25f;
        _tween.TweenProperty(_label, "modulate:a", _label.Modulate.A, fadeStart);
        _tween.TweenProperty(_label, "modulate:a", 0f, Duration - fadeStart)
            .SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);
        if (kind == DmgKind.Crit)
        {
            _tween.TweenProperty(_label, "rotation", 0f, Duration * 0.35f)
                .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        }
        _tween.Finished += QueueFree;
    }
}
