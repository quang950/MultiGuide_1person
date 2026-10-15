using MultiGuide.Domain.Entities;

namespace MultiGuide.Business.Repositories;

public interface IPlayLogRepository
{
    Task<PlayLog> AddAsync(PlayLog playLog, CancellationToken cancellationToken = default);
}
