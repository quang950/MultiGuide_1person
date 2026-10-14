using Microsoft.EntityFrameworkCore;
using MultiGuide.Business.Repositories;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Repositories;

public class TranslationRepository : ITranslationRepository
{
    private readonly MultiGuideDbContext _context;

    public TranslationRepository(MultiGuideDbContext context)
    {
        _context = context;
    }

    public async Task<PoiTranslation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PoiTranslations.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PoiTranslation>> GetByPoiIdAsync(Guid poiId, CancellationToken cancellationToken = default)
    {
        return await _context.PoiTranslations
            .AsNoTracking()
            .Where(t => t.PoiId == poiId)
            .OrderBy(t => t.LanguageCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid poiId, string languageCode, CancellationToken cancellationToken = default)
    {
        return await _context.PoiTranslations
            .AsNoTracking()
            .AnyAsync(t => t.PoiId == poiId && t.LanguageCode == languageCode, cancellationToken);
    }

    public async Task<PoiTranslation> AddAsync(PoiTranslation translation, CancellationToken cancellationToken = default)
    {
        await _context.PoiTranslations.AddAsync(translation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return translation;
    }

    public async Task UpdateAsync(PoiTranslation translation, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(translation);
        if (entry.State == EntityState.Detached)
        {
            _context.PoiTranslations.Update(translation);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(PoiTranslation translation, CancellationToken cancellationToken = default)
    {
        _context.PoiTranslations.Remove(translation);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
