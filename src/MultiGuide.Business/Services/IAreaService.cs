using MultiGuide.Business.DTOs.Client;

namespace MultiGuide.Business.Services;

public interface IAreaService
{
    Task<AreaDetailResponse> GetAreaByCodeAsync(string code, CancellationToken cancellationToken = default);
}
