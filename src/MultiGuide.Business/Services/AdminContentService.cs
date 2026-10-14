using System.Text.RegularExpressions;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Repositories;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Services;

public class AdminContentService : IAdminContentService
{
    private static readonly Regex CodeRegex = new(@"^[a-z0-9-]+$", RegexOptions.Compiled);
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;
    private const int MaxTitleLength = 200;
    private const int MaxLanguageCodeLength = 10;

    private readonly IAreaRepository _areaRepository;
    private readonly IPoiRepository _poiRepository;
    private readonly ITranslationRepository _translationRepository;
    private readonly ILanguageRepository _languageRepository;

    public AdminContentService(
        IAreaRepository areaRepository,
        IPoiRepository poiRepository,
        ITranslationRepository translationRepository,
        ILanguageRepository languageRepository)
    {
        _areaRepository = areaRepository;
        _poiRepository = poiRepository;
        _translationRepository = translationRepository;
        _languageRepository = languageRepository;
    }

    public async Task<IReadOnlyList<AreaResponse>> GetAreasAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var areas = await _areaRepository.GetAllAsync(isActive, cancellationToken);
        return areas.Select(MapToResponse).ToList();
    }

    public async Task<AreaResponse> GetAreaByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var area = await _areaRepository.GetByIdAsync(id, cancellationToken);
        if (area == null)
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, $"Không tìm thấy khu với mã định danh '{id}'.");
        }

        return MapToResponse(area);
    }

    public async Task<AreaResponse> CreateAreaAsync(CreateAreaRequest request, CancellationToken cancellationToken = default)
    {
        ValidateCreateRequest(request);

        var codeNormalized = request.Code.Trim();
        var isDuplicate = await _areaRepository.ExistsByCodeAsync(codeNormalized, null, cancellationToken);
        if (isDuplicate)
        {
            throw new ConflictException(ErrorCodes.DuplicateRequest, $"Khu với mã '{codeNormalized}' đã tồn tại.");
        }

        var area = new TourArea
        {
            Id = Guid.NewGuid(),
            Code = codeNormalized,
            Name = request.Name.Trim(),
            CenterLat = request.CenterLat,
            CenterLng = request.CenterLng,
            RadiusM = request.RadiusM,
            PriceUsd = request.PriceUsd,
            PassTtlHours = request.PassTtlHours,
            IsActive = true
        };

        var created = await _areaRepository.AddAsync(area, cancellationToken);
        return MapToResponse(created);
    }

    public async Task<AreaResponse> UpdateAreaAsync(Guid id, UpdateAreaRequest request, CancellationToken cancellationToken = default)
    {
        var area = await _areaRepository.GetByIdAsync(id, cancellationToken);
        if (area == null)
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, $"Không tìm thấy khu với mã định danh '{id}'.");
        }

        ValidateUpdateRequest(request);

        area.Name = request.Name.Trim();
        area.CenterLat = request.CenterLat;
        area.CenterLng = request.CenterLng;
        area.RadiusM = request.RadiusM;
        area.PriceUsd = request.PriceUsd;
        area.PassTtlHours = request.PassTtlHours;
        area.IsActive = request.IsActive;

        await _areaRepository.UpdateAsync(area, cancellationToken);
        return MapToResponse(area);
    }

    public async Task DeleteAreaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var area = await _areaRepository.GetByIdAsync(id, cancellationToken);
        if (area == null)
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, $"Không tìm thấy khu với mã định danh '{id}'.");
        }

        area.IsActive = false;
        await _areaRepository.UpdateAsync(area, cancellationToken);
    }

    public async Task<PagedResponse<PoiResponse>> GetPoisAsync(Guid? areaId, bool? isActive, int page = 1, int pageSize = DefaultPageSize, CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Trang (page) phải lớn hơn hoặc bằng 1.");
        }

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ValidationException(ErrorCodes.ValidationError, $"Kích thước trang (page_size) phải nằm trong khoảng [1, {MaxPageSize}].");
        }

        var (items, total) = await _poiRepository.GetPagedAsync(areaId, isActive, page, pageSize, cancellationToken);

        return new PagedResponse<PoiResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PoiResponse> GetPoiByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var poi = await _poiRepository.GetByIdAsync(id, cancellationToken);
        if (poi == null)
        {
            throw new NotFoundException(ErrorCodes.NotFound, $"Không tìm thấy điểm POI với mã định danh '{id}'.");
        }

        return MapToResponse(poi);
    }

    public async Task<PoiResponse> CreatePoiAsync(CreatePoiRequest request, CancellationToken cancellationToken = default)
    {
        ValidatePoiFields(request.Lat, request.Lng, request.TriggerRadiusM, request.Priority, request.ImageUrl);

        var area = await _areaRepository.GetByIdAsync(request.AreaId, cancellationToken);
        if (area == null)
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, $"Không tìm thấy khu với mã định danh '{request.AreaId}'.");
        }

        var now = DateTime.UtcNow;
        var poi = new Poi
        {
            Id = Guid.NewGuid(),
            AreaId = request.AreaId,
            Lat = request.Lat,
            Lng = request.Lng,
            TriggerRadiusM = request.TriggerRadiusM,
            Priority = request.Priority,
            ImageUrl = NormalizeImageUrl(request.ImageUrl),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _poiRepository.AddAsync(poi, cancellationToken);
        return MapToResponse(created);
    }

    public async Task<PoiResponse> UpdatePoiAsync(Guid id, UpdatePoiRequest request, CancellationToken cancellationToken = default)
    {
        var poi = await _poiRepository.GetByIdAsync(id, cancellationToken);
        if (poi == null)
        {
            throw new NotFoundException(ErrorCodes.NotFound, $"Không tìm thấy điểm POI với mã định danh '{id}'.");
        }

        ValidatePoiFields(request.Lat, request.Lng, request.TriggerRadiusM, request.Priority, request.ImageUrl);

        poi.Lat = request.Lat;
        poi.Lng = request.Lng;
        poi.TriggerRadiusM = request.TriggerRadiusM;
        poi.Priority = request.Priority;
        poi.ImageUrl = NormalizeImageUrl(request.ImageUrl);
        poi.IsActive = request.IsActive;
        poi.UpdatedAt = DateTime.UtcNow;

        await _poiRepository.UpdateAsync(poi, cancellationToken);
        return MapToResponse(poi);
    }

    public async Task DeletePoiAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var poi = await _poiRepository.GetByIdAsync(id, cancellationToken);
        if (poi == null)
        {
            throw new NotFoundException(ErrorCodes.NotFound, $"Không tìm thấy điểm POI với mã định danh '{id}'.");
        }

        poi.IsActive = false;
        poi.UpdatedAt = DateTime.UtcNow;
        await _poiRepository.UpdateAsync(poi, cancellationToken);
    }

    private static void ValidatePoiFields(double lat, double lng, double triggerRadiusM, int priority, string? imageUrl)
    {
        if (double.IsNaN(lat) || lat is < -90 or > 90)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Vĩ độ (lat) phải nằm trong khoảng [-90, 90].");
        }

        if (double.IsNaN(lng) || lng is < -180 or > 180)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Kinh độ (lng) phải nằm trong khoảng [-180, 180].");
        }

        if (double.IsNaN(triggerRadiusM) || triggerRadiusM is < 5 or > 200)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Bán kính kích hoạt (trigger_radius_m) phải nằm trong khoảng [5, 200].");
        }

        if (priority is < 0 or > 100)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Độ ưu tiên (priority) phải nằm trong khoảng [0, 100].");
        }

        if (imageUrl != null && imageUrl.Trim().Length > 500)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Đường dẫn ảnh (image_url) không được vượt quá 500 ký tự.");
        }
    }

    private static string? NormalizeImageUrl(string? imageUrl) =>
        string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

    private static PoiResponse MapToResponse(Poi poi) => new()
    {
        Id = poi.Id,
        AreaId = poi.AreaId,
        Lat = poi.Lat,
        Lng = poi.Lng,
        TriggerRadiusM = poi.TriggerRadiusM,
        Priority = poi.Priority,
        ImageUrl = poi.ImageUrl,
        IsActive = poi.IsActive,
        CreatedAt = poi.CreatedAt,
        UpdatedAt = poi.UpdatedAt
    };

    public async Task<IReadOnlyList<TranslationResponse>> GetTranslationsAsync(Guid poiId, CancellationToken cancellationToken = default)
    {
        await EnsurePoiExistsAsync(poiId, cancellationToken);

        var translations = await _translationRepository.GetByPoiIdAsync(poiId, cancellationToken);
        return translations.Select(MapToResponse).ToList();
    }

    public async Task<TranslationResponse> GetTranslationByIdAsync(Guid poiId, Guid id, CancellationToken cancellationToken = default)
    {
        var translation = await GetTranslationOfPoiAsync(poiId, id, cancellationToken);
        return MapToResponse(translation);
    }

    public async Task<TranslationResponse> CreateTranslationAsync(Guid poiId, CreateTranslationRequest request, CancellationToken cancellationToken = default)
    {
        var languageCode = (request.LanguageCode ?? string.Empty).Trim().ToLowerInvariant();
        if (languageCode.Length == 0 || languageCode.Length > MaxLanguageCodeLength)
        {
            throw new ValidationException(ErrorCodes.ValidationError, $"Mã ngôn ngữ (language_code) là bắt buộc và tối đa {MaxLanguageCodeLength} ký tự.");
        }

        ValidateTranslationText(request.Title, request.Description);

        await EnsurePoiExistsAsync(poiId, cancellationToken);

        var language = await _languageRepository.GetByCodeAsync(languageCode, cancellationToken);
        if (language == null || !language.IsActive)
        {
            throw new ValidationException(ErrorCodes.ValidationError, $"Ngôn ngữ '{languageCode}' không tồn tại hoặc đã bị tắt.");
        }

        if (await _translationRepository.ExistsAsync(poiId, languageCode, cancellationToken))
        {
            throw new ConflictException(ErrorCodes.TranslationExists, $"POI '{poiId}' đã có bản dịch cho ngôn ngữ '{languageCode}'.");
        }

        var translation = new PoiTranslation
        {
            Id = Guid.NewGuid(),
            PoiId = poiId,
            LanguageCode = languageCode,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _translationRepository.AddAsync(translation, cancellationToken);
        return MapToResponse(created);
    }

    public async Task<TranslationResponse> UpdateTranslationAsync(Guid poiId, Guid id, UpdateTranslationRequest request, CancellationToken cancellationToken = default)
    {
        var translation = await GetTranslationOfPoiAsync(poiId, id, cancellationToken);

        ValidateTranslationText(request.Title, request.Description);

        translation.Title = request.Title.Trim();
        translation.Description = request.Description.Trim();
        translation.UpdatedAt = DateTime.UtcNow;

        await _translationRepository.UpdateAsync(translation, cancellationToken);
        return MapToResponse(translation);
    }

    public async Task DeleteTranslationAsync(Guid poiId, Guid id, CancellationToken cancellationToken = default)
    {
        var translation = await GetTranslationOfPoiAsync(poiId, id, cancellationToken);
        await _translationRepository.DeleteAsync(translation, cancellationToken);
    }

    private async Task EnsurePoiExistsAsync(Guid poiId, CancellationToken cancellationToken)
    {
        var poi = await _poiRepository.GetByIdAsync(poiId, cancellationToken);
        if (poi == null)
        {
            throw new NotFoundException(ErrorCodes.NotFound, $"Không tìm thấy điểm POI với mã định danh '{poiId}'.");
        }
    }

    private async Task<PoiTranslation> GetTranslationOfPoiAsync(Guid poiId, Guid id, CancellationToken cancellationToken)
    {
        await EnsurePoiExistsAsync(poiId, cancellationToken);

        var translation = await _translationRepository.GetByIdAsync(id, cancellationToken);
        if (translation == null || translation.PoiId != poiId)
        {
            throw new NotFoundException(ErrorCodes.NotFound, $"Không tìm thấy bản dịch '{id}' của POI '{poiId}'.");
        }

        return translation;
    }

    private static void ValidateTranslationText(string? title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Tiêu đề (title) là bắt buộc.");
        }

        if (title.Trim().Length > MaxTitleLength)
        {
            throw new ValidationException(ErrorCodes.ValidationError, $"Tiêu đề (title) không được vượt quá {MaxTitleLength} ký tự.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Mô tả (description) là bắt buộc.");
        }
    }

    private static TranslationResponse MapToResponse(PoiTranslation translation) => new()
    {
        Id = translation.Id,
        PoiId = translation.PoiId,
        LanguageCode = translation.LanguageCode,
        Title = translation.Title,
        Description = translation.Description,
        AudioPath = translation.AudioPath,
        AudioDurationSec = translation.AudioDurationSec,
        UpdatedAt = translation.UpdatedAt
    };

    private static void ValidateCreateRequest(CreateAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Mã khu (code) là bắt buộc.");
        }

        var trimmedCode = request.Code.Trim();
        if (trimmedCode.Length > 50)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Mã khu (code) không được vượt quá 50 ký tự.");
        }

        if (!CodeRegex.IsMatch(trimmedCode))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Mã khu (code) chỉ được chứa chữ cái thường, số và dấu gạch ngang (-).");
        }

        ValidateCommonFields(request.Name, request.PriceUsd, request.CenterLat, request.CenterLng, request.RadiusM, request.PassTtlHours);
    }

    private static void ValidateUpdateRequest(UpdateAreaRequest request)
    {
        ValidateCommonFields(request.Name, request.PriceUsd, request.CenterLat, request.CenterLng, request.RadiusM, request.PassTtlHours);
    }

    private static void ValidateCommonFields(string name, decimal priceUsd, double centerLat, double centerLng, double radiusM, int passTtlHours)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Tên khu (name) là bắt buộc.");
        }

        if (name.Trim().Length > 200)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Tên khu (name) không được vượt quá 200 ký tự.");
        }

        if (priceUsd <= 0)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Giá vé (price_usd) phải lớn hơn 0.");
        }

        if (centerLat is < -90 or > 90)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Vĩ độ trung tâm (center_lat) phải nằm trong khoảng [-90, 90].");
        }

        if (centerLng is < -180 or > 180)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Kinh độ trung tâm (center_lng) phải nằm trong khoảng [-180, 180].");
        }

        if (radiusM <= 0)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Bán kính hoạt động (radius_m) phải lớn hơn 0.");
        }

        if (passTtlHours <= 0)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Thời hạn hiệu lực vé (pass_ttl_hours) phải lớn hơn 0.");
        }
    }

    private static AreaResponse MapToResponse(TourArea area) => new()
    {
        Id = area.Id,
        Code = area.Code,
        Name = area.Name,
        CenterLat = area.CenterLat,
        CenterLng = area.CenterLng,
        RadiusM = area.RadiusM,
        PriceUsd = area.PriceUsd,
        PassTtlHours = area.PassTtlHours,
        IsActive = area.IsActive
    };
}
