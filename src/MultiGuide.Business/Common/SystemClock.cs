namespace MultiGuide.Business.Common;

/// <summary>
/// Hiện thực IClock sử dụng DateTime.UtcNow của hệ thống.
/// </summary>
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
