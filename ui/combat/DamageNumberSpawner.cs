using Godot;

/// <summary>
/// DamageNumberSpawner
///
/// Helper static để spawn damage number.
/// EnemyCombat chỉ gọi class này.
/// </summary>
public static class DamageNumberSpawner
{
    private static PackedScene _scene;

    public static void Init()
    {
        if (_scene == null)
        {
            _scene = GD.Load<PackedScene>(
                "res://ui/combat/DamageNumber.tscn"
            );
        }
    }

    public static void Spawn(
        int damage,
        Vector2 worldPos,
        bool isCrit = false,
        bool isBlocked = false)
    {
        if (_scene == null)
            Init();

        var instance = _scene.Instantiate<DamageNumber>();
        var tree = Engine.GetMainLoop() as SceneTree;
        var root = tree.Root;

        root.AddChild(instance);
        instance.GlobalPosition = worldPos;

        instance.Play(damage, isCrit, isBlocked);
    }
}
