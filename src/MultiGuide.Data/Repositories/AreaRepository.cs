using Microsoft.EntityFrameworkCore;
using MultiGuide.Business.Repositories;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Repositories;

public class AreaRepository : IAreaRepository
{
    private readonly MultiGuideDbContext _context;

    public AreaRepository(MultiGuideDbContext context)
    {
        _context = context;
    }

    public async Task<TourArea?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TourAreas
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<TourArea?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.TourAreas
            .FirstOrDefaultAsync(a => a.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<TourArea>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TourAreas.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(a => a.IsActive == isActive.Value);
        }

        return await query.OrderBy(a => a.Name).ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.TourAreas.AsNoTracking().Where(a => a.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<TourArea> AddAsync(TourArea area, CancellationToken cancellationToken = default)
    {
        await _context.TourAreas.AddAsync(area, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return area;
    }

    public async Task UpdateAsync(TourArea area, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(area);
        if (entry.State == EntityState.Detached)
        {
            _context.TourAreas.Update(area);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
