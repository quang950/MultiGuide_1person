namespace MultiGuide.Business.Services;

/// <summary>
/// Theo dõi và đếm số lượng du khách đang truy cập trực tuyến (In-Memory Sliding Window).
/// </summary>
public interface IVisitorTracker
{
    /// <summary>
    /// Ghi nhận thời điểm tương tác gần nhất của du khách thông qua mã vé AccessPass.
    /// </summary>
    void RecordHeartbeat(Guid passId);

    /// <summary>
    /// Lấy số lượng du khách đang hoạt động trong khoảng thời gian cửa sổ trượt (mặc định 30 giây).
    /// </summary>
    int GetActiveVisitorCount(TimeSpan? window = null);
}
