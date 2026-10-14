namespace MultiGuide.Business.Common;

/// <summary>
/// Cung cấp thời gian hệ thống, hỗ trợ mock thời gian trong kiểm thử.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
