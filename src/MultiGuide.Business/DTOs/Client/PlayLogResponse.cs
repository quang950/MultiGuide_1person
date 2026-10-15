namespace MultiGuide.Business.DTOs.Client;

/// <summary>
/// Kết quả ghi nhận nhật ký nghe thuyết minh âm thanh (US-08).
/// </summary>
public class PlayLogResponse
{
    public Guid Id { get; set; }
    public Guid PassId { get; set; }
    public Guid PoiId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public int ListenedSec { get; set; }
    public bool Completed { get; set; }
}
