using Godot;
using System;

public partial class PlayerAttack : Node
{
    private Player _player;
    private PlayerMovement _movement;
    private PlayerAnimation _animation;
    private bool _isAttacking = false;
    public bool IsAttacking => _isAttacking;
    private float _attackDuration = 0.6f;
    private float _timer;

    // Gắn reference tới Player, Movement và Animation
    public void Setup(Player player, PlayerMovement movement, PlayerAnimation animation)
    {
        _player = player;
        _movement = movement;
        _animation = animation;
    }

    // Xử lý logic tấn công theo thời gian
    public void Tick(bool attackInput, double delta)
    {
        if (_isAttacking)
        {
            _timer -= (float)delta;
            if (_timer <= 0f)
                EndAttack();

            return;
        }

        if (attackInput)
            StartAttack();
    }

    // Bắt đầu trạng thái tấn công
    private void StartAttack()
    {
        _isAttacking = true;
        _timer = _attackDuration;

        _movement.SetCanMove(false);
        _animation.PlayAttack(_player.Velocity, _player.Level);
    }

    // Kết thúc tấn công, trả lại quyền di chuyển
    private void EndAttack()
    {
        _isAttacking = false;
        _movement.SetCanMove(true);
    }
}
