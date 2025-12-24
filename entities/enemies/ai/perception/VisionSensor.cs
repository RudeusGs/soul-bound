using Godot;
using System;

/// <summary>
/// VisionSensor
///
/// Cảm biến thị giác (Perception Sensor) của Enemy.
///
/// Vai trò:
/// - Phát hiện các thực thể đi vào vùng nhìn (Area2D)
/// - Phát sinh PerceptionEvent để EnemyBrain / EnemyMemory xử lý
///
/// VisionSensor KHÔNG:
/// - Không quyết định hành vi (chase, attack, investigate)
/// - Không trực tiếp thay đổi Blackboard
/// - Không xử lý logic "mất dấu" (lost sight)
///
/// Thiết kế này tuân theo nguyên tắc:
///     Perception → Memory → Decision → Action
///
/// Trong đó:
/// - VisionSensor chỉ chịu trách nhiệm Perception
/// - Memory + Brain chịu trách nhiệm suy luận và quên dần
/// </summary>
public partial class VisionSensor : Area2D
{
    /// <summary>
    /// (Hiện chưa sử dụng)
    /// Path tới node Blackboard nếu cần mở rộng
    /// VisionSensor hiện tại KHÔNG truy cập trực tiếp Blackboard.
    /// </summary>
    [Export] public NodePath BlackboardPath = "../EnemyBlackboardNode";

    /// <summary>
    /// Nếu true, chỉ phát hiện các Node nằm trong GroupName.
    /// Thường dùng để chỉ detect player.
    /// </summary>
    [Export] public bool OnlyPlayerGroup = true;

    /// <summary>
    /// Tên group được phép detect.
    /// Mặc định là "player".
    /// </summary>
    [Export] public string GroupName = "player";

    /// <summary>
    /// Sự kiện perception được phát khi một thực thể được nhìn thấy.
    ///
    /// EnemyBrain sẽ subscribe event này và chuyển tiếp
    /// cho EnemyMemory (OnSee).
    /// </summary>
    public event Action<PerceptionEvent> Perceived;

    /// <summary>
    /// Được gọi khi node sẵn sàng.
    /// Gắn các signal BodyEntered / BodyExited.
    /// </summary>
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }
    public void RescanNow()
    {
        foreach (var body in GetOverlappingBodies())
            OnBodyEntered(body);
    }

    /// <summary>
    /// Được gọi khi có body đi vào vùng nhìn.
    /// Nếu body hợp lệ (Node2D + đúng group),
    /// phát PerceptionEvent với:
    /// - actor     : thực thể được nhìn thấy
    /// - position  : vị trí hiện tại
    /// - strength  : 1.0 (thấy rõ)
    /// - isVisual  : true
    /// </summary>
    private void OnBodyEntered(Node body)
    {
        if (body is not Node2D n2d)
            return;

        if (OnlyPlayerGroup && !n2d.IsInGroup(GroupName))
            return;

        Perceived?.Invoke(
            new PerceptionEvent(
                n2d,
                n2d.GlobalPosition,
                1.0f,
                isVisual: true
            )
        );
    }

    /// <summary>
    /// Được gọi khi body rời khỏi vùng nhìn.
    ///
    /// Cố ý KHÔNG phát event "Lost" ở đây.
    ///
    /// Lý do thiết kế:
    /// - VisionSensor chỉ báo "đã từng thấy"
    /// - Việc "mất dấu" được xử lý gián tiếp thông qua:
    ///     + EnemyMemory.Tick() (LoseSightTimer)
    ///     + Decay Suspicion / Alertness
    ///
    /// Cách này giúp:
    /// - Tránh AI phản ứng quá máy móc (vừa ra khỏi Area là quên ngay)
    /// - Tạo hành vi giống con người: vẫn đuổi theo trí nhớ một lúc
    /// </summary>
    private void OnBodyExited(Node body)
    {
        if (body is not Node2D n2d) return;
        if (OnlyPlayerGroup && !n2d.IsInGroup(GroupName)) return;

        // Emit "mất thấy" bằng strength = 0
        Perceived?.Invoke(new PerceptionEvent(n2d, n2d.GlobalPosition, 0.0f, isVisual: true));
    }
}
