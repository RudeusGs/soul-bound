using Godot;

/// <summary>
/// MemoryConfig
///
/// Resource cấu hình cho hệ thống trí nhớ (EnemyMemory) của Enemy AI.
///
/// MemoryConfig cho phép tinh chỉnh hành vi AI theo dữ liệu (data-driven),
/// thay vì hard-code trong logic:
/// - AI dễ/khó phát hiện player
/// - Mức nghi ngờ tăng nhanh hay chậm
/// - Quên mục tiêu nhanh hay lâu
///
/// Thường được gán cho EnemyBrain hoặc EnemyMemory thông qua Inspector.
///
/// Lưu ý:
/// - Đây là dữ liệu cấu hình, KHÔNG chứa logic
/// - Có thể tạo nhiều MemoryConfig khác nhau cho từng loại Enemy
/// </summary>
[GlobalClass]
public partial class MemoryConfig : Resource
{
    /// <summary>
    /// Lượng Suspicion tăng khi Enemy nhìn thấy target.
    ///
    /// Giá trị gợi ý:
    /// - 0.8 – 1.0 : Enemy rất nhạy (thấy là nghi ngờ cao)
    /// - 0.4 – 0.6 : Bình thường
    /// - < 0.3     : Enemy chậm phản ứng
    /// </summary>
    [Export] public float SuspicionGainOnSee = 0.8f;

    /// <summary>
    /// Lượng Suspicion tăng khi Enemy nghe thấy âm thanh.
    ///
    /// Thường thấp hơn GainOnSee vì hearing kém chính xác hơn vision.
    /// </summary>
    [Export] public float SuspicionGainOnHear = 0.35f;

    /// <summary>
    /// Tốc độ giảm Suspicion mỗi giây.
    ///
    /// Giá trị lớn → Enemy nhanh quên, dễ bỏ chase.
    /// Giá trị nhỏ → Enemy dai, nghi ngờ lâu.
    /// </summary>
    [Export] public float SuspicionDecayPerSec = 0.12f;

    /// <summary>
    /// Tốc độ giảm Alertness mỗi giây.
    ///
    /// Alertness thường phản ánh trạng thái căng thẳng tức thời,
    /// nên decay chậm hơn Suspicion.
    /// </summary>
    [Export] public float AlertnessDecayPerSec = 0.05f;

    /// <summary>
    /// Thời gian (giây) trước khi Enemy quên hoàn toàn
    /// vị trí nghi ngờ cuối cùng (LastKnownTargetPos).
    ///
    /// Sau thời gian này, Enemy sẽ không investigate nữa
    /// và quay về Patrol / ReturnHome.
    /// </summary>
    [Export] public float ForgetPosAfterSec = 4.0f;
}
