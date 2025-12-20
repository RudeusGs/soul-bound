using Godot;

public partial class EnemyCombat : Node
{
    private Enemy _enemy;

    [Export] public float AttackRange = 30f;
    [Export] public float AttackCooldown = 1.0f;

    private double _cd;

    public void Setup(Enemy enemy) => _enemy = enemy;

    public void Tick(double delta)
    {
        if (_cd > 0) _cd -= delta;
    }

    public bool CanAttack(Node2D target)
        => _cd <= 0 && target != null && target.IsInsideTree();

    public bool IsInRange(Node2D target)
    {
        if (target == null) return false;
        return _enemy.GlobalPosition.DistanceTo(target.GlobalPosition) <= AttackRange;
    }

    public void DoAttack(Node2D target)
    {
        if (!CanAttack(target) || !IsInRange(target)) return;

        _cd = AttackCooldown;

        // TODO: gọi vào hệ thống damage
        // target.GetNode<Health>("Health").TakeDamage(...)
    }
}
