using MultiGuide.Domain.Entities;

namespace MultiGuide.Business.Repositories;

public interface IAreaRepository
{
    Task<TourArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TourArea?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TourArea>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<TourArea> AddAsync(TourArea area, CancellationToken cancellationToken = default);
    Task UpdateAsync(TourArea area, CancellationToken cancellationToken = default);
}
