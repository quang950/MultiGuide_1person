namespace MultiGuide.Business.DTOs.Client;

/// <summary>
/// Ngữ cảnh xác thực của khách tham quan giữ vé AccessPass hợp lệ (US-03).
/// </summary>
public class PassContext
{
    public Guid PassId { get; set; }
    public Guid AreaId { get; set; }
    public DateTime ExpiresAt { get; set; }
}
