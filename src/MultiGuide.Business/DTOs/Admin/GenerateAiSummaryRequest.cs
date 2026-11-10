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

    /// <summary>
    /// Chủ đề hoặc tên POI tùy chỉnh (ví dụ: Bến Bạch Đằng, Khuê Văn Các...).
    /// </summary>
    public string? Topic { get; set; }
}
