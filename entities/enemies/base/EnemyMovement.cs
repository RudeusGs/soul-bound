using Godot;

public partial class EnemyMovement : Node
{
    private Enemy _enemy;
    private Vector2 _desiredVelocity = Vector2.Zero;

    public void Setup(Enemy enemy) => _enemy = enemy;

    public void SetDesiredVelocity(Vector2 v) => _desiredVelocity = v;

    public void Stop() => _desiredVelocity = Vector2.Zero;

    public void Tick(double delta)
    {
        _enemy.Velocity = _desiredVelocity;
        _enemy.MoveAndSlide();
    }
}
