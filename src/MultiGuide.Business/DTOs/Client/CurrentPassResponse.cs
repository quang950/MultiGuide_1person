namespace MultiGuide.Business.DTOs.Client;

/// <summary>
/// Thông tin vé AccessPass hiện tại của khách tham quan (US-04).
/// </summary>
public class CurrentPassResponse
{
    public Guid Id { get; set; }
    public Guid AreaId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int RemainingSeconds { get; set; }
}
