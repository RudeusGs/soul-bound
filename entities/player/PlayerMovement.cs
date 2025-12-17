using Godot;

public partial class PlayerMovement : Node
{
    private Player _player;

    private float _speedMultiplier = 1f;
    private bool _canMove = true;

    public void Setup(Player player)
    {
        _player = player;
    }

    public void Tick(Vector2 inputDir, double delta)
    {
        if (!_canMove)
        {
            _player.Velocity = Vector2.Zero;
            _player.MoveAndSlide();
            return;
        }

        Vector2 velocity = inputDir.Normalized()
            * _player.BaseMoveSpeed
            * _speedMultiplier;

        _player.Velocity = velocity;
        _player.MoveAndSlide();
    }

    // ===== EFFECT API =====

    public void SetCanMove(bool value)
    {
        _canMove = value;
    }

    public void ApplySlow(float percent)
    {
        _speedMultiplier = 1f - percent;
    }

    public void ApplyHaste(float percent)
    {
        _speedMultiplier = 1f + percent;
    }

    public void ResetSpeed()
    {
        _speedMultiplier = 1f;
    }
}
