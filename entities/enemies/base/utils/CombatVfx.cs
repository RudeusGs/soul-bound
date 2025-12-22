using Godot;

/// <summary>
/// CombatVfx
///
/// Các hiệu ứng hình ảnh ngắn hạn khi combat xảy ra.
/// </summary>
public static class CombatVfx
{
    private const string TweenKey = "__hit_flash_tween";

    public static void HitFlash(
        CanvasItem sprite,
        float duration = 0.08f,
        float intensity = 1.0f)
    {
        if (sprite == null || !sprite.IsInsideTree())
            return;

        // Nếu đang có tween cũ → stop lại
        if (sprite.HasMeta(TweenKey))
        {
            var oldTween = sprite.GetMeta(TweenKey).As<Tween>();
            oldTween?.Kill();
            sprite.RemoveMeta(TweenKey);
        }

        Color original = sprite.Modulate;
        Color flash = original.Lerp(Colors.White, Mathf.Clamp(intensity, 0f, 1f));

        sprite.Modulate = flash;

        var tween = sprite.CreateTween();
        tween.TweenProperty(sprite, "modulate", original, duration)
             .SetTrans(Tween.TransitionType.Linear);

        // Lưu tween để lần sau có thể kill
        sprite.SetMeta(TweenKey, tween);

        // Cleanup khi tween xong
        tween.Finished += () =>
        {
            if (sprite.IsInsideTree())
                sprite.RemoveMeta(TweenKey);
        };
    }
}
