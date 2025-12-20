using Godot;

/// <summary>
/// PerceptionEvent
///
/// Struct đại diện cho một "sự kiện nhận thức" (perception) của Enemy.
///
/// PerceptionEvent được tạo ra bởi các sensor (VisionSensor, HearingSensor, ...),
/// sau đó được gửi tới EnemyBrain / EnemyMemory để xử lý.
///
/// Mục đích:
/// - Tách dữ liệu perception khỏi logic xử lý
/// - Cho phép mở rộng nhiều loại sensor mà không đổi kiến trúc
///
/// Luồng sử dụng:
/// Sensor → PerceptionEvent → EnemyBrain → EnemyMemory → Decision (Utility / FSM)
///
/// Lưu ý:
/// - PerceptionEvent là immutable (readonly struct)
/// - Không nên chứa logic trong struct này
/// - Chỉ mang dữ liệu thô về nhận thức
/// </summary>
public readonly struct PerceptionEvent
{
    /// <summary>
    /// Thực thể đã được nhận thức (thường là player).
    /// Có thể null trong một số sensor (ví dụ: tiếng động).
    /// </summary>
    public readonly Node2D Actor;

    /// <summary>
    /// Vị trí phát sinh perception.
    /// Với Vision: vị trí hiện tại của Actor.
    /// Với Hearing: vị trí nguồn âm thanh.
    /// </summary>
    public readonly Vector2 Position;

    /// <summary>
    /// Độ mạnh của perception (0..1).
    ///
    /// Ví dụ:
    /// - 1.0 : thấy rất rõ / tiếng động lớn
    /// - 0.5 : thấy thoáng qua / tiếng động vừa
    /// - 0.0 : gần như không đáng kể
    /// </summary>
    public readonly float Strength;

    /// <summary>
    /// Phân loại perception.
    /// true  : thị giác (Vision)
    /// false : thính giác hoặc loại khác
    /// </summary>
    public readonly bool IsVisual;

    /// <summary>
    /// Tạo một PerceptionEvent mới.
    ///
    /// actor    : thực thể được nhận thức
    /// pos      : vị trí perception
    /// strength : độ mạnh (0..1)
    /// isVisual : true nếu là thị giác
    /// </summary>
    public PerceptionEvent(Node2D actor, Vector2 pos, float strength, bool isVisual)
    {
        Actor = actor;
        Position = pos;
        Strength = strength;
        IsVisual = isVisual;
    }
}
