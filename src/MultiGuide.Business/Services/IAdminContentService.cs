using MultiGuide.Business.DTOs.Admin;

namespace MultiGuide.Business.Services;

public interface IAdminContentService
{
    Task<IReadOnlyList<AreaResponse>> GetAreasAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task<AreaResponse> GetAreaByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AreaResponse> CreateAreaAsync(CreateAreaRequest request, CancellationToken cancellationToken = default);
    Task<AreaResponse> UpdateAreaAsync(Guid id, UpdateAreaRequest request, CancellationToken cancellationToken = default);
    Task DeleteAreaAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(byte[] PngBytes, string FileName)> GenerateAreaQrCodeAsync(Guid areaId, string? publicWebUrl = null, CancellationToken cancellationToken = default);

    Task<PagedResponse<PoiResponse>> GetPoisAsync(Guid? areaId, bool? isActive, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<PoiResponse> GetPoiByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PoiResponse> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default);
    Task<PoiResponse> UpdatePoiAsync(Guid id, UpdatePoiRequest request, CancellationToken cancellationToken = default);
    Task DeletePoiAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TranslationResponse>> GetTranslationsAsync(Guid poiId, CancellationToken cancellationToken = default);
    Task<TranslationResponse> GetTranslationByIdAsync(Guid poiId, Guid id, CancellationToken cancellationToken = default);
    Task<TranslationResponse> CreateTranslationAsync(Guid poiId, CreateTranslationRequest request, CancellationToken cancellationToken = default);
    Task<TranslationResponse> UpdateTranslationAsync(Guid poiId, Guid id, UpdateTranslationRequest request, CancellationToken cancellationToken = default);
    Task DeleteTranslationAsync(Guid poiId, Guid id, CancellationToken cancellationToken = default);

    Task<TranslationResponse> UploadTranslationAudioAsync(
        Guid translationId,
        Stream audioStream,
        long contentLength,
        string? contentType,
        CancellationToken cancellationToken = default);
}
