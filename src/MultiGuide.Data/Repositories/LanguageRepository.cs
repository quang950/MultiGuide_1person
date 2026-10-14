using Microsoft.EntityFrameworkCore;
using MultiGuide.Business.Repositories;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Repositories;

public class LanguageRepository : ILanguageRepository
{
    private readonly MultiGuideDbContext _context;

    public LanguageRepository(MultiGuideDbContext context)
    {
        _context = context;
    }

    public async Task<Language?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Languages
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Code == code, cancellationToken);
    }
}
