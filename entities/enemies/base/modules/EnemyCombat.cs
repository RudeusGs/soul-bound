using Godot;

public partial class EnemyCombat : Node
{
    private Enemy _enemy;

    [Export] public float AttackRange = 30f;
    [Export] public float AttackCooldown = 1.0f;
    [Export] public float AttackEnterRange = 24f;
    private double _cd;

    public void Setup(Enemy enemy)
    {
        _enemy = enemy;
        AttackEnterRange = Mathf.Clamp(AttackEnterRange, 0f, AttackRange);
    }
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
    public bool IsInEnterRange(Node2D target)
    {
        if (target == null) return false;
        float enter = AttackEnterRange > 0f ? AttackEnterRange : AttackRange;
        enter = Mathf.Clamp(enter, 0f, AttackRange);
        return _enemy.GlobalPosition.DistanceTo(target.GlobalPosition) <= enter;
    }
}
