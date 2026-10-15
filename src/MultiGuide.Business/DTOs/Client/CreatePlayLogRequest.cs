namespace MultiGuide.Business.DTOs.Client;

/// <summary>
/// Yêu cầu ghi nhận nhật ký nghe thuyết minh âm thanh (US-08).
/// </summary>
public class CreatePlayLogRequest
{
    public Guid PoiId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public int ListenedSec { get; set; }
    public bool Completed { get; set; }
}
