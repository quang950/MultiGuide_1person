using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Repositories;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Services;

public class AreaService : IAreaService
{
    private readonly IAreaRepository _areaRepository;
    private readonly IGeoService? _geoService;

    public AreaService(IAreaRepository areaRepository, IGeoService? geoService = null)
    {
        _areaRepository = areaRepository;
        _geoService = geoService;
    }

    public async Task<IReadOnlyList<AreaSummaryDto>> GetActiveAreasAsync(
        double? lat = null,
        double? lng = null,
        double? radiusKm = null,
        CancellationToken cancellationToken = default)
    {
        var areas = await _areaRepository.GetAllAsync(isActive: true, cancellationToken);
        var list = areas.Select(a =>
        {
            double? dist = null;
            if (lat.HasValue && lng.HasValue)
            {
                if (_geoService != null)
                {
                    dist = _geoService.CalculateHaversineDistanceM(lat.Value, lng.Value, a.CenterLat, a.CenterLng);
                }
                else
                {
                    dist = CalculateHaversine(lat.Value, lng.Value, a.CenterLat, a.CenterLng);
                }
            }

            return new AreaSummaryDto
            {
                Id = a.Id,
                Code = a.Code,
                Name = a.Name,
                CenterLat = a.CenterLat,
                CenterLng = a.CenterLng,
                RadiusM = a.RadiusM,
                PriceUsd = a.PriceUsd,
                PassTtlHours = a.PassTtlHours,
                DistanceMeters = dist.HasValue ? Math.Round(dist.Value, 1) : null
            };
        }).ToList();

        if (radiusKm.HasValue && radiusKm.Value > 0 && lat.HasValue && lng.HasValue)
        {
            var maxM = radiusKm.Value * 1000.0;
            list = list.Where(a => a.DistanceMeters.HasValue && a.DistanceMeters.Value <= maxM).ToList();
        }

        if (lat.HasValue && lng.HasValue)
        {
            list = list.OrderBy(a => a.DistanceMeters ?? double.MaxValue).ToList();
        }

        return list;
    }

    private static double CalculateHaversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    public async Task<AreaDetailResponse> GetAreaByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, "Mã khu tham quan không được để trống.");
        }

        var normalizedCode = code.Trim().ToLowerInvariant();
        var area = await _areaRepository.GetByCodeAsync(normalizedCode, cancellationToken);

        if (area == null || !area.IsActive)
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, $"Khu tham quan với mã '{code}' không tồn tại hoặc đã ngừng hoạt động.");
        }

        var languages = await _areaRepository.GetSupportedLanguagesAsync(area.Id, cancellationToken);

        return new AreaDetailResponse
        {
            Id = area.Id,
            Code = area.Code,
            Name = area.Name,
            CenterLat = area.CenterLat,
            CenterLng = area.CenterLng,
            RadiusM = area.RadiusM,
            PriceUsd = area.PriceUsd,
            PassTtlHours = area.PassTtlHours,
            SupportedLanguages = languages.Select(l => new SupportedLanguageDto
            {
                Code = l.Code,
                Name = l.Name
            }).ToList()
        };
    }

    public async Task<IReadOnlyList<AreaPoiSummaryDto>> GetAreaPoisAsync(string code, string? lang = "vi", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, "Mã khu tham quan không được để trống.");
        }

        var normalizedCode = code.Trim().ToLowerInvariant();
        var area = await _areaRepository.GetByCodeAsync(normalizedCode, cancellationToken);
        if (area == null || !area.IsActive)
        {
            throw new NotFoundException(ErrorCodes.AreaNotFound, $"Khu tham quan với mã '{code}' không tồn tại hoặc đã ngừng hoạt động.");
        }

        var pois = await _areaRepository.GetPoisByAreaIdAsync(area.Id, isActive: true, cancellationToken);
        var targetLang = string.IsNullOrWhiteSpace(lang) ? "vi" : lang.Trim().ToLowerInvariant();

        var result = new List<AreaPoiSummaryDto>();
        var order = 1;

        foreach (var poi in pois)
        {
            var trans = poi.Translations.FirstOrDefault(t => t.LanguageCode.Equals(targetLang, StringComparison.OrdinalIgnoreCase))
                ?? poi.Translations.FirstOrDefault(t => t.LanguageCode.Equals("vi", StringComparison.OrdinalIgnoreCase))
                ?? poi.Translations.FirstOrDefault();

            var transEn = poi.Translations.FirstOrDefault(t => t.LanguageCode.Equals("en", StringComparison.OrdinalIgnoreCase));

            result.Add(new AreaPoiSummaryDto
            {
                Id = poi.Id,
                Order = order++,
                Priority = poi.Priority,
                Title = trans?.Title ?? "Điểm Di Tích",
                TitleEn = transEn?.Title,
                Lat = poi.Lat,
                Lng = poi.Lng,
                TriggerRadiusM = poi.TriggerRadiusM,
                ImageUrl = poi.ImageUrl,
                Summary = trans?.Description
            });
        }

        return result;
    }
}
