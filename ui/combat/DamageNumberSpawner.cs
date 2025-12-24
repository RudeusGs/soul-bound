using Godot;

public static class DamageNumberSpawner
{
    private static PackedScene _scene;

    public static void Init()
    {
        _scene ??= GD.Load<PackedScene>("res://ui/combat/DamageNumber.tscn");
    }

    /// <summary>
    /// Spawn DamageNumber lên scene hiện tại (an toàn hơn add vào Root).
    /// </summary>
    public static void Spawn(
        int value,
        Vector2 worldPos,
        DamageNumber.DmgKind kind)
    {
        if (_scene == null) Init();

        var tree = Engine.GetMainLoop() as SceneTree;
        var parent = tree?.CurrentScene ?? tree?.Root;
        if (parent == null) return;

        var instance = _scene.Instantiate<DamageNumber>();
        parent.AddChild(instance);

        instance.GlobalPosition = worldPos;
        instance.Play(value, kind);
    }

    public static void SpawnNormal(int value, Vector2 pos) => Spawn(value, pos, DamageNumber.DmgKind.Normal);
    public static void SpawnCrit(int value, Vector2 pos) => Spawn(value, pos, DamageNumber.DmgKind.Crit);
    public static void SpawnTrue(int value, Vector2 pos) => Spawn(value, pos, DamageNumber.DmgKind.True);
    public static void SpawnMiss(Vector2 pos) => Spawn(0, pos, DamageNumber.DmgKind.Miss);
}
