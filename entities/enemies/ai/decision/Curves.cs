using Godot;

/// <summary>
/// Curves
///
/// Tập hợp các hàm "curve" / "response function" dùng trong Utility AI.
///
/// Curves được dùng để:
/// - Biến đổi giá trị thô (0..1) thành hành vi "tự nhiên" hơn
/// - Điều chỉnh mức độ ưu tiên của các UtilityAction
/// - Tránh quyết định tuyến tính, tạo cảm giác AI có cá tính
///
/// Ví dụ sử dụng:
/// - Làm cho Attack chỉ thắng khi Suspicion thật cao
/// - Làm cho ReturnHome chỉ xảy ra khi đi rất xa
///
/// Curves KHÔNG:
/// - Không chứa logic game
/// - Không phụ thuộc vào Enemy hay Blackboard
/// </summary>
public static class Curves
{
    /// <summary>
    /// InverseLerp
    ///
    /// Chuyển giá trị v trong khoảng [a, b] về khoảng chuẩn [0..1].
    ///
    /// - v <= a → 0
    /// - v >= b → 1
    /// - v ở giữa → nội suy tuyến tính
    ///
    /// Dùng phổ biến cho:
    /// - Tính độ "xa" so với ngưỡng
    /// - Quy đổi khoảng cách thành điểm Utility
    /// </summary>
    public static float InverseLerp(float a, float b, float v)
    {
        if (Mathf.IsEqualApprox(a, b))
            return 0f;

        return Mathf.Clamp((v - a) / (b - a), 0f, 1f);
    }

    /// <summary>
    /// Ramp
    ///
    /// Đảm bảo giá trị nằm trong khoảng [0..1].
    ///
    /// Thường dùng như một hàm "an toàn" để clamp score Utility.
    /// </summary>
    public static float Ramp(float x)
        => Mathf.Clamp(x, 0f, 1f);

    /// <summary>
    /// Sharp
    ///
    /// Làm đường cong trở nên "sắc" hơn bằng cách bình phương giá trị.
    ///
    /// - x nhỏ → càng nhỏ hơn (đè các giá trị thấp)
    /// - x gần 1 → giữ gần 1
    ///
    /// Dùng khi:
    /// - Muốn hành vi chỉ xảy ra khi giá trị thật cao
    /// - Ví dụ: Attack chỉ khi Suspicion rất lớn
    /// </summary>
    public static float Sharp(float x)
        => x * x;
}
