using Microsoft.EntityFrameworkCore;
using MultiGuide.Business.Repositories;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Repositories;

public class PoiRepository : IPoiRepository
{
    private readonly MultiGuideDbContext _context;

    public PoiRepository(MultiGuideDbContext context)
    {
        _context = context;
    }

    public async Task<Poi?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Pois.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Poi?> GetWithTranslationsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Pois
            .AsNoTracking()
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<Poi> Items, int TotalCount)> GetPagedAsync(
        Guid? areaId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Pois.AsNoTracking();

        if (areaId.HasValue)
        {
            query = query.Where(p => p.AreaId == areaId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Poi>> GetActiveByAreaIdAsync(Guid areaId, CancellationToken cancellationToken = default)
    {
        return await _context.Pois
            .AsNoTracking()
            .Include(p => p.Translations)
            .Where(p => p.AreaId == areaId && p.IsActive)
            .OrderBy(p => p.Priority)
            .ToListAsync(cancellationToken);
    }

    public async Task<Poi> AddAsync(Poi poi, CancellationToken cancellationToken = default)
    {
        await _context.Pois.AddAsync(poi, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return poi;
    }

    public async Task UpdateAsync(Poi poi, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(poi);
        if (entry.State == EntityState.Detached)
        {
            _context.Pois.Update(poi);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
