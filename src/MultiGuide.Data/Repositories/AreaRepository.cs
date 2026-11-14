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

    public async Task DeleteAsync(TourArea area, CancellationToken cancellationToken = default)
    {
        var pois = await _context.Pois
            .Where(p => p.AreaId == area.Id)
            .ToListAsync(cancellationToken);

        var poiIds = pois.Select(p => p.Id).ToList();

        if (poiIds.Count > 0)
        {
            var playLogs = await _context.PlayLogs
                .Where(p => poiIds.Contains(p.PoiId))
                .ToListAsync(cancellationToken);
            if (playLogs.Count > 0)
            {
                _context.PlayLogs.RemoveRange(playLogs);
            }

            var translations = await _context.PoiTranslations
                .Where(t => poiIds.Contains(t.PoiId))
                .ToListAsync(cancellationToken);
            if (translations.Count > 0)
            {
                _context.PoiTranslations.RemoveRange(translations);
            }

            _context.Pois.RemoveRange(pois);
        }

        var passes = await _context.AccessPasses
            .Where(p => p.AreaId == area.Id)
            .ToListAsync(cancellationToken);
        if (passes.Count > 0)
        {
            _context.AccessPasses.RemoveRange(passes);
        }

        var payments = await _context.Payments
            .Where(p => p.AreaId == area.Id)
            .ToListAsync(cancellationToken);
        if (payments.Count > 0)
        {
            _context.Payments.RemoveRange(payments);
        }

        _context.TourAreas.Remove(area);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Language>> GetSupportedLanguagesAsync(Guid areaId, CancellationToken cancellationToken = default)
    {
        return await _context.Languages
            .AsNoTracking()
            .Where(l => l.IsActive && l.Translations.Any(t => t.Poi != null && t.Poi.AreaId == areaId && t.Poi.IsActive))
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Poi>> GetPoisByAreaIdAsync(Guid areaId, bool? isActive = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Pois
            .Include(p => p.Translations)
            .AsNoTracking()
            .Where(p => p.AreaId == areaId);

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
