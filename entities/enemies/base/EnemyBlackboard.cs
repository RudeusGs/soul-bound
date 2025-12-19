using Godot;
using System.Collections.Generic;

public enum FacingDir { Down, Up, Left, Right }

public class EnemyBlackboard
{
    public Node2D Target;
    public Node2D LastAttacker;

    private readonly HashSet<ulong> _aggroIds = new();

    public bool IsAggroOn(Node2D t)
        => t != null && _aggroIds.Contains(t.GetInstanceId());

    public void RememberAggro(Node2D t)
    {
        if (t == null) return;
        _aggroIds.Add(t.GetInstanceId());
    }

    public bool HasAnyAggro => _aggroIds.Count > 0;

    public bool IsAttacking;
    public bool IsDead;

    // NEW: hướng để idle đúng
    public FacingDir Facing = FacingDir.Down;
}
