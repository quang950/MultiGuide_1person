namespace MultiGuide.Business.Services;

/// <summary>
/// Interface dịch vụ Trí tuệ nhân tạo (AI Service) - T-4.4 / US-19.
/// Không phụ thuộc vào bất kỳ thư viện HTTP hay hạ tầng cụ thể nào để tuân thủ kiến trúc 3 lớp.
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Sinh đoạn văn tóm tắt lịch sử / câu chuyện thuyết minh (150-200 từ) cho một điểm tham quan (POI).
    /// </summary>
    /// <param name="poiName">Tên điểm tham quan</param>
    /// <param name="areaName">Tên khu vực / khu di tích</param>
    /// <param name="language">Mã ngôn ngữ đích (vi, en, ja...)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Nội dung thuyết minh do AI sinh ra</returns>
    Task<string> GeneratePoiSummaryAsync(
        string poiName,
        string areaName,
        string language = "vi",
        CancellationToken cancellationToken = default);
}
