using MultiGuide.Domain.Entities;

namespace MultiGuide.Business.Repositories;

public interface ITranslationRepository
{
    Task<PoiTranslation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PoiTranslation>> GetByPoiIdAsync(Guid poiId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid poiId, string languageCode, CancellationToken cancellationToken = default);
    Task<PoiTranslation> AddAsync(PoiTranslation translation, CancellationToken cancellationToken = default);
    Task UpdateAsync(PoiTranslation translation, CancellationToken cancellationToken = default);
    Task DeleteAsync(PoiTranslation translation, CancellationToken cancellationToken = default);
}
