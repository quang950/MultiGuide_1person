using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Storage;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Services;

public class ContentService : IContentService
{
    private readonly IPoiRepository _poiRepository;
    private readonly IFileStorage _fileStorage;

    public ContentService(IPoiRepository poiRepository, IFileStorage fileStorage)
    {
        _poiRepository = poiRepository;
        _fileStorage = fileStorage;
    }

    public async Task<PoiDetailResponse> GetPoiDetailAsync(
        Guid poiId,
        Guid passAreaId,
        string? preferredLanguage = null,
        CancellationToken cancellationToken = default)
    {
        var poi = await _poiRepository.GetWithTranslationsByIdAsync(poiId, cancellationToken);
        if (poi == null || !poi.IsActive)
        {
            throw new NotFoundException(
                ErrorCodes.PoiContentNotFound,
                $"Điểm tham quan '{poiId}' không tồn tại hoặc đã ngừng hoạt động.");
        }

        if (poi.AreaId != passAreaId)
        {
            throw new ForbiddenException(
                ErrorCodes.PassAreaMismatch,
                "AccessPass không thuộc khu vực của điểm tham quan này.");
        }

        if (poi.Translations == null || poi.Translations.Count == 0)
        {
            throw new NotFoundException(
                ErrorCodes.PoiContentNotFound,
                $"Điểm tham quan '{poiId}' chưa có bản dịch nội dung.");
        }

        // Logic fallback (US-06): preferredLanguage -> en -> vi
        PoiTranslation? selectedTranslation = null;
        string? fallbackLanguage = null;

        var hasPreferredLang = !string.IsNullOrWhiteSpace(preferredLanguage);
        var normPreferred = preferredLanguage?.Trim().ToLowerInvariant();

        if (hasPreferredLang)
        {
            selectedTranslation = poi.Translations.FirstOrDefault(t =>
                string.Equals(t.LanguageCode, normPreferred, StringComparison.OrdinalIgnoreCase));
        }

        if (selectedTranslation == null)
        {
            // Thử fallback sang English
            selectedTranslation = poi.Translations.FirstOrDefault(t =>
                string.Equals(t.LanguageCode, "en", StringComparison.OrdinalIgnoreCase));
            if (selectedTranslation != null && hasPreferredLang && !string.Equals(normPreferred, "en", StringComparison.OrdinalIgnoreCase))
            {
                fallbackLanguage = "en";
            }
        }

        if (selectedTranslation == null)
        {
            // Thử fallback sang Tiếng Việt
            selectedTranslation = poi.Translations.FirstOrDefault(t =>
                string.Equals(t.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase));
            if (selectedTranslation != null && hasPreferredLang && !string.Equals(normPreferred, "vi", StringComparison.OrdinalIgnoreCase))
            {
                fallbackLanguage = "vi";
            }
        }

        if (selectedTranslation == null)
        {
            throw new NotFoundException(
                ErrorCodes.PoiContentNotFound,
                $"Điểm tham quan '{poiId}' không có bản dịch phù hợp.");
        }

        // Tạo signed URL audio nếu có file audio (thời hạn 1 giờ theo US-07)
        string? audioUrl = null;
        if (!string.IsNullOrWhiteSpace(selectedTranslation.AudioPath))
        {
            audioUrl = await _fileStorage.CreateSignedUrlAsync(
                selectedTranslation.AudioPath,
                TimeSpan.FromHours(1),
                cancellationToken);
        }

        return new PoiDetailResponse
        {
            Id = poi.Id,
            AreaId = poi.AreaId,
            Title = selectedTranslation.Title,
            Description = selectedTranslation.Description,
            Language = selectedTranslation.LanguageCode,
            FallbackLanguage = fallbackLanguage,
            AudioUrl = audioUrl,
            AudioDurationSec = selectedTranslation.AudioDurationSec,
            ImageUrl = poi.ImageUrl,
            Lat = poi.Lat,
            Lng = poi.Lng,
            TriggerRadiusM = poi.TriggerRadiusM
        };
    }
}
