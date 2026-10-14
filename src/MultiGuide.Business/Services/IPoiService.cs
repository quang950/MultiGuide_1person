using MultiGuide.Business.DTOs.Client;

namespace MultiGuide.Business.Services;

/// <summary>
/// Dịch vụ tra cứu và tìm kiếm điểm tham quan cho du khách (US-05).
/// </summary>
public interface IPoiService
{
    /// <summary>
    /// Tìm danh sách POI lân cận vị trí GPS của du khách thuộc khu vực của AccessPass (US-05).
    /// </summary>
    Task<IReadOnlyList<NearbyPoiResponse>> GetNearbyPoisAsync(
        Guid areaId,
        double lat,
        double lng,
        double radiusM,
        string? preferredLanguage = null,
        CancellationToken cancellationToken = default);
}
