using Godot;

public partial class PlayerMovement : Node
{
    private Player _player;

    private float _speedMultiplier = 1f;
    private bool _canMove = true;

    // Gán reference Player để điều khiển vận tốc và di chuyển
    public void Setup(Player player)
    {
        _player = player;
    }

    // Xử lý di chuyển nhân vật dựa trên input và trạng thái chạy
    public void Tick(Vector2 inputDir, bool isRunning, double delta)
    {
        if (!_canMove)
        {
            _player.Velocity = Vector2.Zero;
            _player.MoveAndSlide();
            return;
        }

        float runMultiplier = isRunning ? 1.5f : 1f;

        Vector2 velocity = inputDir.Normalized()
            * _player.BaseMoveSpeed
            * _speedMultiplier
            * runMultiplier;

        _player.Velocity = velocity;
        _player.MoveAndSlide();
    }

    // Bật / tắt khả năng di chuyển
    public void SetCanMove(bool value)
    {
        _canMove = value;
    }

    // Áp hiệu ứng làm chậm tốc độ di chuyển
    public void ApplySlow(float percent)
    {
        _speedMultiplier = 1f - percent;
    }

    // Áp hiệu ứng tăng tốc độ di chuyển
    public void ApplyHaste(float percent)
    {
        _speedMultiplier = 1f + percent;
    }

    // Reset tốc độ về mặc định
    public void ResetSpeed()
    {
        _speedMultiplier = 1f;
    }
}
