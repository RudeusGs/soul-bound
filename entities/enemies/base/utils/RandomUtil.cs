using Godot;

public static class RandomUtil
{
    /// <summary>
    /// Random point đồng đều theo diện tích trong vòng tròn.
    /// </summary>
    public static Vector2 PointInRadius(Vector2 center, float radius)
    {
        var angle = (float)GD.RandRange(0, Mathf.Tau);
        var r = radius * Mathf.Sqrt((float)GD.Randf());
        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
    }
}
