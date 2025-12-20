using System;
using System.Collections.Generic;

/// <summary>
/// Simple finite state machine (FSM) dùng cho Enemy AI.
///
/// Vai trò:
/// - Quản lý các state hành vi (Idle, Patrol, Chase, Attack, ...)
/// - Đảm bảo tại mỗi thời điểm chỉ có MỘT state được active
/// - Ngăn việc chuyển lại cùng state liên tục (tránh rung animation / flip-flop hành vi)
///
/// Nguyên tắc sử dụng:
/// - EnemyBrain / Utility AI quyết định "nên làm gì"
/// - StateMachine chỉ chịu trách nhiệm "chuyển và chạy state"
/// - Mỗi state được tạo 1 lần và tái sử dụng
///
/// Lưu ý quan trọng:
/// - Change<T>() sẽ KHÔNG gọi Exit/Enter nếu đang ở đúng state
/// - Điều này rất quan trọng để tránh spam Enter/Exit mỗi frame
/// </summary>
public sealed class StateMachine
{
    /// <summary>
    /// Danh sách toàn bộ state có thể dùng.
    /// Key: typeof(StateClass)
    /// Value: instance của state đó
    /// </summary>
    private readonly Dictionary<Type, IEnemyState> _states = new();

    /// <summary>
    /// Kiểu (Type) của state hiện tại.
    /// Dùng để so sánh nhanh và chặn việc đổi lại cùng state.
    /// </summary>
    private Type _currentType;

    /// <summary>
    /// State đang active.
    /// Chỉ được thay đổi thông qua Change&lt;T&gt;().
    /// </summary>
    public IEnemyState Current { get; private set; }

    /// <summary>
    /// Đăng ký một state vào StateMachine.
    /// Mỗi state chỉ cần Add MỘT lần khi khởi tạo EnemyBrain.
    /// </summary>
    public void Add<T>(T state) where T : IEnemyState
        => _states[typeof(T)] = state;

    /// <summary>
    /// Chuyển sang state T.
    ///
    /// - Nếu đang ở state T → KHÔNG làm gì (return false)
    /// - Nếu khác state → gọi Exit() state cũ, Enter() state mới
    ///
    /// Trả về:
    /// - true  : có chuyển state thật sự
    /// - false : không chuyển (đang ở state này rồi)
    ///
    /// Lý do cần return bool:
    /// - EnemyBrain có thể dùng để debounce / lock state change
    /// </summary>
    public bool Change<T>() where T : IEnemyState
    {
        var t = typeof(T);
        if (_currentType == t)
            return false;

        Current?.Exit();
        Current = _states[t];
        _currentType = t;
        Current.Enter();
        return true;
    }

    /// <summary>
    /// Gọi Tick() của state hiện tại.
    /// Được gọi mỗi frame (thường trong _PhysicsProcess).
    /// </summary>
    public void Tick(double delta)
        => Current?.Tick(delta);
}
