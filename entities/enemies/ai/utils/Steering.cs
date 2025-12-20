using Godot;

/// <summary>
/// Steering
///
/// Tập hợp các hàm "steering behavior" cơ bản cho Enemy AI.
///
/// Steering behaviors dùng để:
/// - Tính hướng di chuyển (direction) một cách mượt
/// - Tách thuật toán điều hướng khỏi logic state
/// - Dùng chung cho nhiều state (Patrol, Investigate, Chase, ReturnHome...)
///
/// Hiện tại chỉ có Seek(),
/// nhưng có thể mở rộng thêm:
/// - Arrive (giảm tốc khi tới gần)
/// - Flee (chạy xa target)
/// - Wander (đi lang thang)
/// - Pursue / Evade (đuổi / né có dự đoán)
/// </summary>
public static class Steering
{
    /// <summary>
    /// Seek
    ///
    /// Tính vector hướng từ điểm `from` tới điểm `to`.
    ///
    /// - Nếu khoảng cách rất nhỏ → trả về Vector2.Zero
    /// - Ngược lại → trả về vector đơn vị (Normalized)
    ///
    /// Lưu ý:
    /// - Hàm này KHÔNG nhân tốc độ
    /// - State / Movement chịu trách nhiệm nhân WalkSpeed / RunSpeed
    ///
    /// Ví dụ:
    /// var dir = Steering.Seek(enemyPos, targetPos);
    /// move.SetDesiredVelocity(dir * enemy.RunSpeed);
    /// </summary>
    public static Vector2 Seek(Vector2 from, Vector2 to)
    {
        var d = to - from;

        // Nếu quá gần → không cần di chuyển
        if (d.Length() < 0.001f)
            return Vector2.Zero;

        return d.Normalized();
    }
}
