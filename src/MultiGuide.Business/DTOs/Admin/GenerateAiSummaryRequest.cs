namespace MultiGuide.Business.DTOs.Admin;

/// <summary>
/// Yêu cầu sinh tóm tắt lịch sử / câu chuyện thuyết minh POI từ AI (US-19).
/// </summary>
public class GenerateAiSummaryRequest
{
    /// <summary>
    /// Ngôn ngữ mong muốn (vi, en, ja... Mặc định: vi).
    /// </summary>
    public string? Language { get; set; } = "vi";
}
