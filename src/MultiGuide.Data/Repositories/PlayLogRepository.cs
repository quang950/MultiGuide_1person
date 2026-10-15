using MultiGuide.Business.Repositories;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Repositories;

public class PlayLogRepository : IPlayLogRepository
{
    private readonly MultiGuideDbContext _context;

    public PlayLogRepository(MultiGuideDbContext context)
    {
        _context = context;
    }

    public async Task<PlayLog> AddAsync(PlayLog playLog, CancellationToken cancellationToken = default)
    {
        await _context.PlayLogs.AddAsync(playLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return playLog;
    }
}
