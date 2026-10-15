namespace MultiGuide.Business.DTOs.Admin;

/// <summary>
/// Kết quả sinh tóm tắt lịch sử / câu chuyện thuyết minh POI từ AI (US-19).
/// </summary>
public class AiSummaryResponse
{
    public required Guid PoiId { get; set; }
    public required string PoiName { get; set; }
    public required string AreaName { get; set; }
    public required string Summary { get; set; }
    public required string Language { get; set; }
    public required string Model { get; set; }
}
