using MultiGuide.Domain.Entities;

namespace MultiGuide.Business.Repositories;

public interface IPoiRepository
{
    Task<Poi?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Poi> Items, int TotalCount)> GetPagedAsync(
        Guid? areaId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Poi>> GetActiveByAreaIdAsync(
        Guid areaId,
        CancellationToken cancellationToken = default);

    Task<Poi> AddAsync(Poi poi, CancellationToken cancellationToken = default);

    Task UpdateAsync(Poi poi, CancellationToken cancellationToken = default);
}
