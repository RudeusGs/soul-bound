// FILE: entities/enemies/base/modules/EnemyCombat.cs
using Godot;
using System;

public partial class EnemyCombat : Node
{
    [ExportGroup("Links")]
    [Export] public NodePath SpritePath = "../AnimatedSprite2D";


    // =========================
    // ATTACK RANGE (KHOẢNG CÁCH)
    // =========================

    [ExportGroup("Attack Range")]

    /// <summary>
    /// AttackRange: khoảng cách tối đa để enemy được phép bắt đầu attack.
    /// - Nếu target ở ngoài AttackRange: DoAttack() sẽ không chạy.
    /// - Đây là “tầm đánh thực tế”.
    /// </summary>
    [Export] public float AttackRange = 30f;

    /// <summary>
    /// AttackEnterRange: khoảng cách để AI “quyết định chuyển sang AttackState”.
    /// - Thường <= AttackRange.
    /// - Tăng giá trị này nếu muốn enemy đứng xa hơn một chút rồi đánh.
    /// - Dùng cho hysteresis/logic decide (UtilityBrain).
    /// </summary>
    [Export] public float AttackEnterRange = 24f;


    // =========================
    // COOLDOWN (HỒI CHIÊU)
    // =========================

    [ExportGroup("Cooldown")]

    /// <summary>
    /// AttackCooldown: thời gian hồi sau mỗi lần enemy “ra đòn”.
    /// - _cd sẽ được set = AttackCooldown khi bắt đầu đánh.
    /// - CanAttack() chỉ true khi _cd <= 0.
    /// </summary>
    [Export] public float AttackCooldown = 1.0f;


    // =========================
    // LOCK-ON (ĐÁNH CHẮC TRÚNG)
    // =========================

    [ExportGroup("Lock-on (Unavoidable)")]

    /// <summary>
    /// LockOnUnavoidableHit:
    /// - true  : enemy “lock” target ngay lúc bắt đầu vung và tới HitFrame sẽ gọi OnHit trực tiếp lên player (khó né).
    /// - false : không gọi OnHit trực tiếp (tuỳ bạn dùng cách gây damage khác).
    /// 
    /// Dùng để tạo cảm giác auto-attack kiểu LoL: đã ra đòn là gần như trúng.
    /// </summary>
    [Export] public bool LockOnUnavoidableHit = true;


    // =========================
    // TIMING THEO FRAME ANIMATION
    // =========================

    [ExportGroup("Timing by Animation Frame")]

    /// <summary>
    /// UseAnimFrameTiming:
    /// - true  : damage nổ đúng frame HitFrame của animation attack (đẹp, đồng bộ vung kiếm).
    /// - false : không phụ thuộc frame (thường dùng hit theo timer/delay).
    /// 
    /// Bạn đang muốn “đúng frame” -> nên để true.
    /// </summary>
    [Export] public bool UseAnimFrameTiming = true;

    /// <summary>
    /// HitFrame (0-based):
    /// Frame index của animation attack mà bạn muốn “damage xảy ra”.
    /// - 0-based: frame đầu tiên là 0.
    /// - Vì 4 hướng có frame giống nhau nên dùng 1 số chung.
    /// 
    /// Ví dụ:
    /// - Frame 0: vừa vào anim
    /// - Frame 1: bắt đầu vung
    /// - Frame 2: chạm (hit)
    /// </summary>
    [Export] public int HitFrame = 2;

    /// <summary>
    /// RequireFrameAdvance:
    /// Dùng để tránh trường hợp “mới vào anim attack, frame reset về 0” mà đã fire hit.
    /// 
    /// - true  : bắt buộc sprite phải chạy qua ít nhất 1 frame (frame thay đổi) trước khi cho phép hit.
    /// - false : cho phép hit ngay khi frame == HitFrame (có thể gây hit sớm nếu HitFrame=0).
    /// 
    /// Khuyên để true để chống bug “frame 0 đã ăn damage”.
    /// </summary>
    [Export] public bool RequireFrameAdvance = true;

    /// <summary>
    /// FallbackFireOnAnimFinish:
    /// Bảo hiểm trường hợp animation attack không bao giờ chạm tới HitFrame (do bị ngắt / loop / tốc độ frame).
    /// 
    /// - true  : nếu kết thúc anim attack mà chưa hit -> fire hit “muộn” ở cuối anim (không bao giờ hit sớm).
    /// - false : nếu không chạm HitFrame thì... không gây damage.
    /// 
    /// Dùng để tránh “enemy đánh mà không trúng bao giờ” khi set HitFrame sai.
    /// </summary>
    [Export] public bool FallbackFireOnAnimFinish = true;

    /// <summary>
    /// AttackAnimTag:
    /// Chuỗi dùng để nhận biết “đây là animation attack”.
    /// EnemyAnimation thường play theo pattern: $"{AnimPrefix}_attack_{dir}"
    /// nên tag mặc định "_attack_" là hợp lý.
    /// 
    /// Ví dụ anim name:
    /// - "slime_attack_down"
    /// - "slime_attack_left"
    /// 
    /// Nếu dự án bạn đặt tên khác, đổi tag này tương ứng.
    /// </summary>
    [Export] public string AttackAnimTag = "_attack_";


    // =========================
    // RECOVERY / SAFETY
    // =========================

    [ExportGroup("Recovery")]

    /// <summary>
    /// PostHitRecovery:
    /// Sau khi đã fire hit (đúng HitFrame), giữ trạng thái “đang attack” thêm một đoạn ngắn.
    /// Mục đích:
    /// - animation nhìn “ra chiêu” đã tay hơn (không bị chuyển ngay sang run/idle).
    /// - tránh AI đổi state quá sớm.
    /// </summary>
    [Export] public float PostHitRecovery = 0.06f;

    /// <summary>
    /// CancelIfTargetFartherThan:
    /// An toàn để hủy hit nếu tới lúc HitFrame mà target đã “biến mất / teleport / dash quá xa”.
    /// 
    /// - 0  : không kiểm tra, đã lock là trúng (đúng kiểu LoL auto attack).
    /// - >0 : nếu khoảng cách enemy-target > giá trị này ở thời điểm hit -> hủy hit.
    /// 
    /// Gợi ý:
    /// - Để 0 nếu bạn muốn “không né được”.
    /// - Để khoảng 1.2x~2x AttackRange nếu bạn muốn vẫn “có giới hạn”.
    /// </summary>
    [Export] public float CancelIfTargetFartherThan = 0f;


    private Enemy _enemy;
    private AnimatedSprite2D _sprite;

    private double _cd;
    private double _postHitTimer;

    private bool _attackPending;
    private bool _hitFired;
    private Node2D _lockedTarget;
    private bool _sawAttackAnim;
    private int _startFrame;
    private bool _frameAdvanced;

    public bool IsSwinging => _attackPending || _postHitTimer > 0;

    public void Setup(Enemy enemy)
    {
        _enemy = enemy;

        AttackEnterRange = Mathf.Clamp(AttackEnterRange, 0f, AttackRange);
        AttackCooldown = Mathf.Max(0f, AttackCooldown);
        PostHitRecovery = Mathf.Max(0f, PostHitRecovery);
        CancelIfTargetFartherThan = Mathf.Max(0f, CancelIfTargetFartherThan);
        HitFrame = Mathf.Max(0, HitFrame);
        if (string.IsNullOrEmpty(AttackAnimTag)) AttackAnimTag = "_attack_";

        _sprite = GetNodeOrNull<AnimatedSprite2D>(SpritePath);
        if (_sprite == null)
        {
            GD.PrintErr("[EnemyCombat] Missing AnimatedSprite2D. Check SpritePath.");
            return;
        }

        _sprite.AnimationChanged += OnSpriteAnimationChanged;
        _sprite.FrameChanged += OnSpriteFrameChanged;
        _sprite.AnimationFinished += OnSpriteAnimationFinished;
    }

    public void Tick(double delta)
    {
        if (_cd > 0) _cd = Math.Max(0, _cd - delta);

        if (_postHitTimer > 0)
        {
            _postHitTimer = Math.Max(0, _postHitTimer - delta);
            if (_enemy?.BB != null) _enemy.BB.IsAttacking = true;
            if (_postHitTimer <= 0 && _enemy?.BB != null) _enemy.BB.IsAttacking = false;
        }
        if (_attackPending && _enemy?.BB != null)
            _enemy.BB.IsAttacking = true;
    }

    public bool CanAttack(Node2D target)
        => target != null
           && target.IsInsideTree()
           && _cd <= 0
           && !_attackPending
           && _postHitTimer <= 0;

    public bool IsInRange(Node2D target)
        => target != null
           && _enemy != null
           && _enemy.GlobalPosition.DistanceTo(target.GlobalPosition) <= AttackRange;

    public bool IsInEnterRange(Node2D target)
        => target != null
           && _enemy != null
           && _enemy.GlobalPosition.DistanceTo(target.GlobalPosition) <= AttackEnterRange;

    public void DoAttack(Node2D target)
    {
        if (!CanAttack(target)) return;
        if (!IsInRange(target)) return;

        _cd = AttackCooldown;
        _lockedTarget = target;

        _attackPending = true;
        _hitFired = false;

        _sawAttackAnim = false;
        _frameAdvanced = false;
        _startFrame = 0;

        if (_enemy?.BB != null) _enemy.BB.IsAttacking = true;
    }

    private void OnSpriteAnimationChanged()
    {
        if (!_attackPending || _sprite == null) return;

        if (LooksLikeAttackAnim(_sprite.Animation))
        {
            _sawAttackAnim = true;
            _startFrame = _sprite.Frame;
            _frameAdvanced = false;
        }
    }

    private void OnSpriteFrameChanged()
    {
        if (!_attackPending || _hitFired || _sprite == null) return;
        if (!UseAnimFrameTiming) return;

        if (!LooksLikeAttackAnim(_sprite.Animation))
            return;

        if (!_sawAttackAnim)
        {
            _sawAttackAnim = true;
            _startFrame = _sprite.Frame;
            _frameAdvanced = false;
        }

        if (_sprite.Frame != _startFrame)
            _frameAdvanced = true;

        if (RequireFrameAdvance && !_frameAdvanced)
            return;

        if (_sprite.Frame == HitFrame)
            FireHit();
    }

    private void OnSpriteAnimationFinished()
    {
        if (!_attackPending || _hitFired || _sprite == null) return;
        if (!UseAnimFrameTiming) return;

        if (!LooksLikeAttackAnim(_sprite.Animation))
            return;
        if (FallbackFireOnAnimFinish)
            FireHit();
        else
            EndSwingNoHit();
    }

    private bool LooksLikeAttackAnim(string anim)
    {
        if (string.IsNullOrEmpty(anim) || string.IsNullOrEmpty(AttackAnimTag)) return false;
        return anim.Contains(AttackAnimTag, StringComparison.OrdinalIgnoreCase);
    }

    private void FireHit()
    {
        _hitFired = true;
        _attackPending = false;

        if (_lockedTarget == null || !_lockedTarget.IsInsideTree() || _enemy == null)
        {
            EndSwingNoHit();
            return;
        }

        if (CancelIfTargetFartherThan > 0f)
        {
            var d = _enemy.GlobalPosition.DistanceTo(_lockedTarget.GlobalPosition);
            if (d > CancelIfTargetFartherThan)
            {
                EndSwingNoHit();
                return;
            }
        }

        if (LockOnUnavoidableHit)
        {
            if (!TryCallOnHit(_lockedTarget))
                TryCallCommonDamageMethods(_lockedTarget);
        }

        _postHitTimer = PostHitRecovery;
        if (_enemy?.BB != null) _enemy.BB.IsAttacking = true;
    }

    private void EndSwingNoHit()
    {
        _attackPending = false;
        _hitFired = true;
        _postHitTimer = 0;
        if (_enemy?.BB != null) _enemy.BB.IsAttacking = false;
    }

    private bool TryCallOnHit(Node2D target)
    {
        if (target.HasMethod("OnHit"))
        {
            target.Call("OnHit", _enemy);
            return true;
        }

        foreach (var childName in new[] { "PlayerHitReceiver", "HitReceiver", "DamageReceiver" })
        {
            var n = target.GetNodeOrNull<Node>(childName);
            if (n != null && n.HasMethod("OnHit"))
            {
                n.Call("OnHit", _enemy);
                return true;
            }
        }

        foreach (var c in target.GetChildren())
        {
            if (c is Node n && n.HasMethod("OnHit"))
            {
                n.Call("OnHit", _enemy);
                return true;
            }
        }

        return false;
    }

    private void TryCallCommonDamageMethods(Node2D target)
    {
        if (target.HasMethod("TakeDamage"))
            target.Call("TakeDamage", 1, _enemy);
        else if (target.HasMethod("ReceiveDamage"))
            target.Call("ReceiveDamage", 1, _enemy);
        else if (target.HasMethod("ApplyDamage"))
            target.Call("ApplyDamage", 1, _enemy);
    }
}
