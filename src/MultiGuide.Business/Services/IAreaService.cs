using MultiGuide.Business.DTOs.Client;

namespace MultiGuide.Business.Services;

public interface IAreaService
{
    Task<AreaDetailResponse> GetAreaByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AreaSummaryDto>> GetActiveAreasAsync(double? lat = null, double? lng = null, double? radiusKm = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AreaPoiSummaryDto>> GetAreaPoisAsync(string code, string? lang = "vi", CancellationToken cancellationToken = default);
}
