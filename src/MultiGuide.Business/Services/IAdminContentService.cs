using MultiGuide.Business.DTOs.Admin;

namespace MultiGuide.Business.Services;

public interface IAdminContentService
{
    Task<IReadOnlyList<AreaResponse>> GetAreasAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<AreaResponse> GetAreaByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AreaResponse> CreateAreaAsync(CreateAreaRequest request, CancellationToken cancellationToken = default);
    Task<AreaResponse> UpdateAreaAsync(Guid id, UpdateAreaRequest request, CancellationToken cancellationToken = default);
    Task DeleteAreaAsync(Guid id, CancellationToken cancellationToken = default);
}
