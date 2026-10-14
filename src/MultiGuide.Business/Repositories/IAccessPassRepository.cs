using MultiGuide.Domain.Entities;

namespace MultiGuide.Business.Repositories;

public interface IAccessPassRepository
{
    Task<AccessPass?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AccessPass?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<AccessPass> AddAsync(AccessPass pass, CancellationToken cancellationToken = default);
    Task UpdateAsync(AccessPass pass, CancellationToken cancellationToken = default);
}
