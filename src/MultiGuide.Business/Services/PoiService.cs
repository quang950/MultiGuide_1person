using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Repositories;

namespace MultiGuide.Business.Services;

public class PoiService : IPoiService
{
    private readonly IPoiRepository _poiRepository;
    private readonly IGeoService _geoService;

    public PoiService(IPoiRepository poiRepository, IGeoService geoService)
    {
        _poiRepository = poiRepository;
        _geoService = geoService;
    }

    public async Task<IReadOnlyList<NearbyPoiResponse>> GetNearbyPoisAsync(
        Guid areaId,
        double lat,
        double lng,
        double radiusM,
        string? preferredLanguage = null,
        CancellationToken cancellationToken = default)
    {
        var pois = await _poiRepository.GetActiveByAreaIdAsync(areaId, cancellationToken);
        return _geoService.FindNearbyPois(pois, lat, lng, radiusM, preferredLanguage);
    }
}
