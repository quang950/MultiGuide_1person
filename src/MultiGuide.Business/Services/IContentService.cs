using MultiGuide.Business.DTOs.Client;

namespace MultiGuide.Business.Services;

/// <summary>
/// Quản lý nội dung thuyết minh POI, logic fallback ngôn ngữ và xin signed URL audio (US-06, US-07).
/// </summary>
public interface IContentService
{
    /// <summary>
    /// Lấy chi tiết nội dung POI và audio signed URL có thời hạn 1 giờ theo ngôn ngữ yêu cầu (kèm fallback en -> vi) (US-06, US-07).
    /// </summary>
    Task<PoiDetailResponse> GetPoiDetailAsync(
        Guid poiId,
        Guid passAreaId,
        string? preferredLanguage = null,
        CancellationToken cancellationToken = default);
}
