/// <summary>
/// UtilityAction
///
/// Enum đại diện cho các hành động cấp cao mà Enemy AI có thể lựa chọn.
///
/// UtilityBrain sẽ:
/// - Tính điểm (score) cho từng UtilityAction
/// - Chọn action có score cao nhất
///
/// EnemyBrain sẽ:
/// - Nhận UtilityAction
/// - Chuyển sang State tương ứng thông qua StateMachine
///
/// Lưu ý:
/// - UtilityAction KHÔNG phải State
/// - Đây là "ý định" (intent), không phải hành vi thực thi
/// </summary>
public enum UtilityAction
{
    /// <summary>
    /// Idle
    ///
    /// Không làm gì.
    /// Thường dùng khi:
    /// - Enemy đã chết
    /// - Hoặc chưa sẵn sàng hành động
    /// </summary>
    Idle,

    /// <summary>
    /// Patrol
    ///
    /// Hành vi tuần tra mặc định.
    /// Được chọn khi:
    /// - Không có target
    /// - Suspicion thấp
    /// </summary>
    Patrol,

    /// <summary>
    /// Investigate
    ///
    /// Hành vi điều tra.
    /// Được chọn khi:
    /// - Không còn thấy target
    /// - Nhưng còn LastKnownTargetPos
    /// - Suspicion / DamageAwareness đủ cao
    /// </summary>
    Investigate,

    /// <summary>
    /// Chase
    ///
    /// Hành vi truy đuổi.
    /// Được chọn khi:
    /// - Có target hợp lệ
    /// - Hoặc vừa mới mất target nhưng còn LoseSightTimer
    /// </summary>
    Chase,

    /// <summary>
    /// Attack
    ///
    /// Hành vi tấn công trực tiếp.
    /// Được chọn khi:
    /// - Target ở trong phạm vi tấn công
    /// - Suspicion cao
    /// </summary>
    Attack,

    /// <summary>
    /// ReturnHome
    ///
    /// Hành vi quay về vị trí ban đầu (home).
    /// Được chọn khi:
    /// - Enemy đi quá xa home
    /// - Và mức Suspicion thấp
    /// </summary>
    ReturnHome
}
