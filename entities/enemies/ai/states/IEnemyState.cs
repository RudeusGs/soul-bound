/// <summary>
/// IEnemyState
///
/// Interface định nghĩa một "State" trong hệ thống Enemy AI.
///
/// Mỗi state đại diện cho MỘT hành vi cụ thể của Enemy
/// (ví dụ: Patrol, Investigate, Chase, Attack, ReturnHome).
///
/// Vòng đời của một state:
/// 1) Enter()  – được gọi MỘT lần khi state được kích hoạt
/// 2) Tick()   – được gọi mỗi frame khi state đang active
/// 3) Exit()   – được gọi MỘT lần khi rời khỏi state
///
/// Nguyên tắc thiết kế:
/// - State KHÔNG tự quyết định chuyển state
/// - State KHÔNG gọi StateMachine.Change()
/// - EnemyBrain / UtilityBrain là nơi duy nhất quyết định chuyển state
///
/// Trách nhiệm của State:
/// - Thực hiện hành vi (move, attack, idle...)
/// - Đọc dữ liệu từ Blackboard
/// - Ghi các flag hành vi cần thiết (IsChasing, IsAttacking...)
///
/// Không nên làm trong State:
/// - Không chứa logic ra quyết định cấp cao
/// - Không quản lý memory / suspicion
/// - Không xử lý perception (vision/hearing)
/// </summary>
public interface IEnemyState
{
    /// <summary>
    /// Được gọi khi state này bắt đầu.
    /// Dùng để:
    /// - Reset flag
    /// - Setup biến tạm
    /// - Chuẩn bị animation / movement
    /// </summary>
    void Enter();

    /// <summary>
    /// Được gọi khi rời khỏi state này.
    /// Dùng để:
    /// - Cleanup
    /// - Reset flag
    /// - Dừng hành vi đang thực hiện
    /// </summary>
    void Exit();

    /// <summary>
    /// Được gọi mỗi frame khi state đang active.
    ///
    /// delta:
    /// - Thời gian (giây) kể từ frame trước
    ///
    /// Thực hiện logic hành vi chính của state.
    /// </summary>
    void Tick(double delta);
}
