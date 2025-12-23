using Godot;

/// <summary>
/// AttackState (Combat Orbit)
/// - Khi cooldown: chạy vòng quanh target (orbit/strafe) để tạo độ khó.
/// - Khi tới lượt đánh: dừng ngắn để ra đòn, rồi tiếp tục chạy vòng.
/// </summary>
public sealed class AttackState : IEnemyState
{
    private readonly Enemy _enemy;
    private readonly EnemyMovement _move;
    private readonly EnemyCombat _combat;
    private readonly EnemyBlackboard _bb;

    // Orbit behavior
    private int _orbitSign = 1;          // +1 / -1
    private double _flipTimer = 0;       // đổi hướng chạy sau mỗi vài giây

    // Attack window
    private double _attackHoldTimer = 0; // dừng ngắn khi vừa đánh (để anim hit)

    // Tuning (bạn chỉnh các số này cho hợp cảm giác)
    private readonly float _strafeSpeedScale; // % RunSpeed
    private readonly float _orbitBand;        // biên dao động quanh khoảng cách mong muốn
    private readonly float _radialWeight;     // lực kéo vào/đẩy ra khi lệch khoảng cách
    private readonly float _attackHoldTime;   // đứng lại bao lâu sau khi đánh

    public AttackState(
        Enemy enemy,
        EnemyMovement move,
        EnemyCombat combat,
        EnemyBlackboard bb,
        float strafeSpeedScale = 0.75f, // chạy vòng nhanh/chậm
        float orbitBand = 4f,           // band càng lớn càng lắc lư xa gần
        float radialWeight = 0.6f,      // kéo vào/đẩy ra mạnh yếu
        float attackHoldTime = 0.12f    // đứng lại sau mỗi hit
    )
    {
        _enemy = enemy;
        _move = move;
        _combat = combat;
        _bb = bb;

        _strafeSpeedScale = strafeSpeedScale;
        _orbitBand = orbitBand;
        _radialWeight = radialWeight;
        _attackHoldTime = attackHoldTime;
    }

    public void Enter()
    {
        // vào combat
        _bb.InCombat = true;

        // Khi không swing thì để run/walk anim; chỉ bật IsAttacking đúng lúc ra đòn
        _bb.IsAttacking = false;

        // cho anim chạy kiểu "run" khi đang combat (nếu bạn muốn walk thì set false)
        _bb.IsChasing = true;

        _orbitSign = GD.Randf() < 0.5f ? -1 : 1;
        _flipTimer = GD.RandRange(0.8, 1.6);
        _attackHoldTimer = 0;
    }

    public void Exit()
    {
        _bb.InCombat = false;
        _bb.IsAttacking = false;
        _move.Stop();
    }

    public void Tick(double delta)
    {
        if (_bb.Target == null || !_bb.Target.IsInsideTree())
        {
            _bb.IsAttacking = false;
            _move.Stop();
            return;
        }

        // đổi hướng orbit thỉnh thoảng để khó đoán
        _flipTimer -= delta;
        if (_flipTimer <= 0)
        {
            if (GD.Randf() < 0.7f) _orbitSign *= -1;
            _flipTimer = GD.RandRange(0.8, 1.6);
        }

        // đang trong window đánh -> đứng yên
        if (_attackHoldTimer > 0)
        {
            _attackHoldTimer -= delta;
            if (_attackHoldTimer <= 0) _bb.IsAttacking = false;

            _move.Stop();
            return;
        }

        Node2D target = _bb.Target;

        // Nếu đến lượt đánh và trong tầm -> đánh
        if (_combat.CanAttack(target) && _combat.IsInRange(target))
        {
            _bb.IsAttacking = true;

            _move.Stop();
            _combat.DoAttack(target);

            _attackHoldTimer = _attackHoldTime;

            // đôi lúc đổi hướng ngay sau mỗi hit
            if (GD.Randf() < 0.35f) _orbitSign *= -1;
            return;
        }

        // ===== Orbit / Strafe khi cooldown =====
        Vector2 to = target.GlobalPosition - _enemy.GlobalPosition;
        float dist = to.Length();
        if (dist < 0.001f)
        {
            _move.Stop();
            return;
        }

        Vector2 dir = to / dist; // hướng tới target
        Vector2 tangent = new Vector2(-dir.Y, dir.X) * _orbitSign; // chạy vòng quanh

        // Khoảng cách mong muốn để "đánh mà không dính sát"
        float desired = _combat.AttackEnterRange > 0f ? _combat.AttackEnterRange : _combat.AttackRange * 0.8f;

        // Sửa khoảng cách: quá sát -> lùi, quá xa -> áp vào
        Vector2 radial = Vector2.Zero;
        if (dist < desired - _orbitBand) radial = -dir;
        else if (dist > desired + _orbitBand) radial = dir;

        // nếu sắp vượt khỏi tầm đánh thì kéo vào mạnh hơn
        if (dist > _combat.AttackRange - 1f) radial = dir;

        Vector2 moveDir = (tangent + radial * _radialWeight).Normalized();
        float speed = _enemy.RunSpeed * _strafeSpeedScale;

        _move.SetDesiredVelocity(moveDir * speed);
    }
}
