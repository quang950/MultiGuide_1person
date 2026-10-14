namespace MultiGuide.Business.DTOs.Client;

/// <summary>
/// Yêu cầu mua AccessPass cho một khu tham quan (US-02).
/// </summary>
public class CreatePassRequest
{
    public string AreaCode { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}

/// <summary>
/// Phản hồi sau khi mua AccessPass thành công (US-02).
/// AccessToken chỉ được trả về một lần duy nhất lúc này.
/// </summary>
public class CreatePassResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public Guid AreaId { get; set; }
    public string AreaCode { get; set; } = string.Empty;
    public decimal PriceUsd { get; set; }
}
