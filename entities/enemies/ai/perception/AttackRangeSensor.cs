using Godot;
using System;

public partial class AttackRangeSensor : Area2D
{
    [Export] public bool OnlyPlayerGroup = true;
    [Export] public string GroupName = "player";

    public event Action<Node2D, bool> InRangeChanged;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    private void OnBodyEntered(Node body)
    {
        if (body is not Node2D n2d) return;
        if (OnlyPlayerGroup && !n2d.IsInGroup(GroupName)) return;

        InRangeChanged?.Invoke(n2d, true);
    }

    private void OnBodyExited(Node body)
    {
        if (body is not Node2D n2d) return;
        if (OnlyPlayerGroup && !n2d.IsInGroup(GroupName)) return;

        InRangeChanged?.Invoke(n2d, false);
    }
}
