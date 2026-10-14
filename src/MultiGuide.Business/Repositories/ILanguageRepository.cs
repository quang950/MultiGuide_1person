using MultiGuide.Domain.Entities;

namespace MultiGuide.Business.Repositories;

public interface ILanguageRepository
{
    Task<Language?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
