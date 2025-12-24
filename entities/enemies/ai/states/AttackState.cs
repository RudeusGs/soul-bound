using Godot;
using System;

public sealed class AttackState : IEnemyState
{
    private readonly Enemy _enemy;
    private readonly EnemyMovement _move;
    private readonly EnemyCombat _combat;
    private readonly EnemyBlackboard _bb;

    private enum Phase
    {
        ReadyToAttack,   // đứng yên chờ tới lượt đánh
        Swinging,        // đang đánh -> đứng yên
        Repositioning,   // chạy quanh player (không đánh)
        Settle           // đứng yên 1 nhịp
    }

    private Phase _phase;

    // =========================
    // TUNING
    // =========================
    private readonly float _repositionSpeedScale;
    private readonly float _repositionTimeMin;
    private readonly float _repositionTimeMax;

    // giữ khoảng cách quanh player trong [minDist, maxDist]
    private readonly float _minDistOverride;
    private readonly float _maxDistOverride;

    private readonly float _settleTime;

    // Nếu EnemyCombat không có “IsSwinging”, dùng fallback timer để thoát Swinging chắc chắn
    private readonly float _fallbackSwingTime;

    // =========================
    // RUNTIME
    // =========================
    private double _swingTimer;

    private double _repositionTimer;
    private int _strafeSign = 1;         // +1/-1 chạy theo chiều kim / ngược kim
    private Vector2 _lastPos;
    private double _stuckTimer;

    private double _settleTimer;

    // ---------- Constructors ----------
    // Khớp EnemyBrain bạn đang gọi: new AttackState(_enemy, _movement, _combat, _bb)
    public AttackState(
        Enemy enemy,
        EnemyMovement move,
        EnemyCombat combat,
        EnemyBlackboard bb,
        float repositionSpeedScale = 0.95f,
        float repositionTimeMin = 0.35f,
        float repositionTimeMax = 0.60f,
        float minDist = 0f,
        float maxDist = 0f,
        float settleTime = 0.06f,
        float fallbackSwingTime = 0.28f
    )
    {
        _enemy = enemy;
        _move = move;
        _combat = combat;
        _bb = bb;

        _repositionSpeedScale = repositionSpeedScale;
        _repositionTimeMin = repositionTimeMin;
        _repositionTimeMax = repositionTimeMax;

        _minDistOverride = minDist;
        _maxDistOverride = maxDist;

        _settleTime = settleTime;
        _fallbackSwingTime = fallbackSwingTime;
    }

    // Khớp signature cũ nếu chỗ nào còn dùng: new AttackState(_movement, _combat, _bb)
    public AttackState(
        EnemyMovement move,
        EnemyCombat combat,
        EnemyBlackboard bb
    ) : this(move?.GetParent() as Enemy, move, combat, bb) { }

    public void Enter()
    {
        _phase = Phase.ReadyToAttack;

        _bb.InCombat = true;
        _bb.IsAttacking = false;
        _bb.IsChasing = false;

        _swingTimer = 0;
        _repositionTimer = 0;
        _stuckTimer = 0;
        _settleTimer = 0;

        _move.Stop();
    }

    public void Exit()
    {
        _bb.InCombat = false;
        _bb.IsAttacking = false;
        _bb.IsChasing = false;

        _move.Stop();
    }

    public void Tick(double delta)
    {
        if (_enemy == null || _combat == null)
        {
            _move.Stop();
            return;
        }

        if (_bb.Target == null || !_bb.Target.IsInsideTree())
        {
            _bb.IsAttacking = false;
            _bb.IsChasing = false;
            _move.Stop();
            return;
        }

        Node2D target = _bb.Target;

        switch (_phase)
        {
            case Phase.Swinging:
                {
                    // ĐANG ĐÁNH -> ĐỨNG YÊN
                    _bb.IsAttacking = true;
                    _bb.IsChasing = false;
                    _move.Stop();

                    _swingTimer -= delta;

                    // Thoát Swinging chắc chắn bằng timer (không bao giờ kẹt)
                    if (_swingTimer <= 0)
                    {
                        _bb.IsAttacking = false;
                        BeginReposition(target);
                    }
                    return;
                }

            case Phase.Repositioning:
                {
                    // ĐANG CHẠY -> KHÔNG ĐÁNH
                    _bb.IsAttacking = false;
                    _bb.IsChasing = true;

                    // giảm thời gian chạy
                    _repositionTimer -= delta;

                    Vector2 enemyPos = _enemy.GlobalPosition;
                    Vector2 targetPos = target.GlobalPosition;

                    // phát hiện kẹt: không di chuyển được
                    float moved = enemyPos.DistanceTo(_lastPos);
                    _lastPos = enemyPos;

                    if (moved < 0.25f) _stuckTimer += delta;
                    else _stuckTimer = 0;

                    // tính khoảng cách mong muốn
                    float desired = _combat.AttackEnterRange > 0f ? _combat.AttackEnterRange : (_combat.AttackRange * 0.8f);

                    float minDist = (_minDistOverride > 0f) ? _minDistOverride : Mathf.Max(12f, desired - 4f);
                    float maxDist = (_maxDistOverride > 0f) ? _maxDistOverride : Mathf.Max(minDist + 6f, desired + 10f);

                    Vector2 toTarget = targetPos - enemyPos;
                    float dist = toTarget.Length();

                    if (dist < 0.001f)
                    {
                        // cực hiếm: trùng vị trí -> đẩy ra hướng bất kỳ
                        Vector2 push = new Vector2(1, 0) * _enemy.RunSpeed * _repositionSpeedScale;
                        _move.SetDesiredVelocity(push);
                    }
                    else
                    {
                        Vector2 dir = toTarget / dist;
                        Vector2 tangent = new Vector2(-dir.Y, dir.X) * _strafeSign;

                        // radial correction để KHÔNG BAO GIỜ dính vào player
                        Vector2 radial = Vector2.Zero;
                        if (dist < minDist) radial = -dir;        // quá gần -> lùi ra
                        else if (dist > maxDist) radial = dir;    // quá xa -> áp vào nhẹ

                        // trộn hướng: ưu tiên chạy vòng, nhưng luôn tránh dính sát
                        Vector2 moveDir = (tangent * 0.85f + radial * 0.55f);

                        if (moveDir.LengthSquared() < 0.0001f)
                            moveDir = tangent;

                        moveDir = moveDir.Normalized();

                        float speed = _enemy.RunSpeed * _repositionSpeedScale;
                        _move.SetDesiredVelocity(moveDir * speed);
                    }

                    // Kết thúc reposition khi:
                    // - hết timer
                    // - hoặc bị kẹt quá lâu (thoát chắc chắn)
                    if (_repositionTimer <= 0 || _stuckTimer >= 0.45)
                    {
                        _move.Stop();
                        _bb.IsChasing = false;

                        _phase = Phase.Settle;
                        _settleTimer = _settleTime;
                    }
                    return;
                }

            case Phase.Settle:
                {
                    // DỪNG 1 NHỊP
                    _bb.IsAttacking = false;
                    _bb.IsChasing = false;
                    _move.Stop();

                    _settleTimer -= delta;
                    if (_settleTimer <= 0)
                        _phase = Phase.ReadyToAttack;

                    return;
                }

            case Phase.ReadyToAttack:
            default:
                {
                    // ĐỨNG YÊN CHỜ ĐÁNH
                    _bb.IsAttacking = false;
                    _bb.IsChasing = false;
                    _move.Stop();

                    if (_combat.CanAttack(target) && _combat.IsInRange(target))
                    {
                        // Bắt đầu đánh
                        _combat.DoAttack(target);

                        // giữ swing trong 1 khoảng cố định (thoát chắc chắn)
                        // (có thể chỉnh fallbackSwingTime cho khớp anim)
                        _swingTimer = _fallbackSwingTime;

                        _phase = Phase.Swinging;
                        return;
                    }

                    return;
                }
        }
    }

    private void BeginReposition(Node2D target)
    {
        _phase = Phase.Repositioning;

        // random hướng chạy vòng
        _strafeSign = GD.Randf() < 0.5f ? -1 : 1;

        // random thời gian chạy (không dùng điểm đích => không thể trúng vị trí player)
        float tmin = Mathf.Max(0.10f, _repositionTimeMin);
        float tmax = Mathf.Max(tmin, _repositionTimeMax);
        _repositionTimer = Mathf.Lerp(tmin, tmax, GD.Randf());

        _lastPos = _enemy.GlobalPosition;
        _stuckTimer = 0;

        _bb.IsChasing = true;
    }
}
