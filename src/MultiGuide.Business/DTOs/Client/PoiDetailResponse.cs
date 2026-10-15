namespace MultiGuide.Business.DTOs.Client;

/// <summary>
/// Chi tiết nội dung điểm tham quan và audio thuyết minh (US-06, US-07).
/// </summary>
public class PoiDetailResponse
{
    public Guid Id { get; set; }
    public Guid AreaId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string? FallbackLanguage { get; set; }
    public string? AudioUrl { get; set; }
    public int? AudioDurationSec { get; set; }
    public string? ImageUrl { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double TriggerRadiusM { get; set; }
}
