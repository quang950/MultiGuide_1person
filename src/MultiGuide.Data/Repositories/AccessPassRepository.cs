using Microsoft.EntityFrameworkCore;
using MultiGuide.Business.Repositories;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Repositories;

public class AccessPassRepository : IAccessPassRepository
{
    private readonly MultiGuideDbContext _context;

    public AccessPassRepository(MultiGuideDbContext context)
    {
        _context = context;
    }

    public async Task<AccessPass?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AccessPasses
            .Include(a => a.Area)
            .Include(a => a.Payment)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<AccessPass?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.AccessPasses
            .Include(a => a.Area)
            .Include(a => a.Payment)
            .FirstOrDefaultAsync(a => a.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<AccessPass> AddAsync(AccessPass pass, CancellationToken cancellationToken = default)
    {
        await _context.AccessPasses.AddAsync(pass, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return pass;
    }

    public async Task UpdateAsync(AccessPass pass, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(pass);
        if (entry.State == EntityState.Detached)
        {
            _context.AccessPasses.Update(pass);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
