using Godot;

public partial class PlayerAttack : Node
{
    private Player _player;
    private PlayerMovement _movement;
    private PlayerAnimation _animation;
    private bool _isAttacking = false;
    public bool IsAttacking => _isAttacking;
    private float _attackDuration = 0.47f;
    private float _cooldown = 0.1f;
    private float _timer;
    private Area2D _activeHitbox;
    private readonly Godot.Collections.Dictionary<string, Area2D> _hitboxes = new();
    private readonly System.Collections.Generic.HashSet<Node> _hitTargets = new();

    [ExportGroup("Hitboxes")]
    [Export] public NodePath HitboxRootPath = "../AttackHitboxes";

    // Gắn reference tới Player, Movement và Animation
    public void Setup(Player player, PlayerMovement movement, PlayerAnimation animation)
    {
        _player = player;
        _movement = movement;
        _animation = animation;

        CacheHitboxes();
    }

    // Xử lý logic tấn công theo thời gian
    public void Tick(bool attackInput, double delta)
    {
        _timer -= (float)delta;

        if (_isAttacking)
        {
            if (_timer <= 0f)
                EndAttack();
            return;
        }

        if (attackInput && _timer <= 0f)
            StartAttack();
    }

    // Bắt đầu trạng thái tấn công
    private void StartAttack()
    {
        _isAttacking = true;
        _timer = _attackDuration;
        _hitTargets.Clear();

        _movement.SetCanMove(false);
        _animation.PlayAttack(_player.Velocity, _player.Level);
        ActivateHitboxForDirection(_animation?.LastDirection ?? "down");
    }

    // Kết thúc tấn công, trả lại quyền di chuyển
    private void EndAttack()
    {
        _isAttacking = false;
        _timer = _cooldown;
        _movement.SetCanMove(true);
        DisableAllHitboxes();
    }

    private void CacheHitboxes()
    {
        _hitboxes.Clear();

        var root = GetNodeOrNull<Node>(HitboxRootPath);
        if (root == null)
        {
            GD.PrintErr("[PlayerAttack] Missing AttackHitboxes node. Check HitboxRootPath.");
            return;
        }

        TryRegisterHitbox(root, "HitboxDown", "down");
        TryRegisterHitbox(root, "HitboxUp", "up");
        TryRegisterHitbox(root, "HitboxLeft", "left");
        TryRegisterHitbox(root, "HitboxRight", "right");

        DisableAllHitboxes();
    }

    private void TryRegisterHitbox(Node root, string nodeName, string dirKey)
    {
        var hitbox = root.GetNodeOrNull<Area2D>(nodeName);
        if (hitbox == null)
        {
            GD.PrintErr($"[PlayerAttack] Missing hitbox {nodeName} under {HitboxRootPath}.");
            return;
        }

        hitbox.Monitoring = false;
        hitbox.BodyEntered += OnHitboxBodyEntered;
        _hitboxes[dirKey] = hitbox;
    }

    private void ActivateHitboxForDirection(string dir)
    {
        DisableAllHitboxes();

        if (string.IsNullOrEmpty(dir) || !_hitboxes.ContainsKey(dir))
            dir = "down";

        _activeHitbox = _hitboxes[dir];
        _activeHitbox.Monitoring = true;
    }

    private void DisableAllHitboxes()
    {
        foreach (var hitbox in _hitboxes.Values)
            hitbox.Monitoring = false;

        _activeHitbox = null;
    }

    private void OnHitboxBodyEntered(Node body)
    {
        if (!_isAttacking || _activeHitbox == null || !_activeHitbox.Monitoring)
            return;

        if (body is not Node2D target)
            return;

        if (target == _player || _hitTargets.Contains(target))
            return;

        if (!IsEnemyTarget(target))
            return;

        if (ApplyHitToTarget(target))
            _hitTargets.Add(target);
    }

    private static bool IsEnemyTarget(Node2D target)
    {
        return target.IsInGroup("enemy") || target is Enemy;
    }

    private bool ApplyHitToTarget(Node2D target)
    {
        if (target is Enemy enemy)
        {
            enemy.OnDamaged(_player);
            FlashEnemySprite(enemy);
            return true;
        }

        return false;
    }

    private void FlashEnemySprite(Enemy enemy)
    {
        var sprite = enemy.GetNodeOrNull<CanvasItem>("AnimatedSprite2D");
        if (sprite != null)
        {
            CombatVfx.HitFlash(sprite);
            return;
        }

        foreach (var child in enemy.GetChildren())
        {
            if (child is CanvasItem canvas)
            {
                CombatVfx.HitFlash(canvas);
                return;
            }
        }
    }
}
