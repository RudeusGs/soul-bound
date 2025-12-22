using Godot;

/// <summary>
/// DamageNumber
///
/// Hiển thị số damage bay lên + scale + fade.
/// Không chứa logic combat.
/// </summary>
public partial class DamageNumber : Node2D
{
    [Export] public float FloatDistance = 20f;
    [Export] public float Duration = 0.6f;

    private Label _label;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
    }

    public void Play(
        int value,
        bool isCrit = false,
        bool isBlocked = false)
    {
        // ===== TEXT =====
        _label.Text = value.ToString();

        // ===== STYLE =====
        if (isBlocked)
        {
            _label.Modulate = Colors.Gray;
            _label.Text = "BLOCK";
        }
        else if (isCrit)
        {
            _label.Modulate = Colors.OrangeRed;
            _label.Scale = Vector2.One * 1.2f;
        }
        else
        {
            _label.Modulate = Colors.White;
        }

        // Random lệch nhẹ cho tự nhiên
        Vector2 offset = new(
            GD.RandRange(-6, 6),
            GD.RandRange(-4, 4)
        );

        Position += offset;

        // ===== TWEEN =====
        var tween = CreateTween();

        // Bay lên
        tween.TweenProperty(
            this,
            "position:y",
            Position.Y - FloatDistance,
            Duration
        ).SetEase(Tween.EaseType.Out);

        // Fade out
        tween.TweenProperty(
            _label,
            "modulate:a",
            0f,
            Duration
        );

        // Scale nhẹ (pop)
        tween.TweenProperty(
            _label,
            "scale",
            Vector2.One,
            Duration * 0.3f
        ).From(_label.Scale * 1.2f);

        tween.Finished += QueueFree;
    }
}
