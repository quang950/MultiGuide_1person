using MultiGuide.Business.DTOs.Client;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Business.Services;

public class GeoService : IGeoService
{
    private const double EarthRadiusM = 6_371_000.0;

    public double CalculateHaversineDistanceM(double lat1, double lng1, double lat2, double lng2)
    {
        if (Math.Abs(lat1 - lat2) < 1e-9 && Math.Abs(lng1 - lng2) < 1e-9)
        {
            return 0.0;
        }

        var dLat = (lat2 - lat1) * (Math.PI / 180.0);
        var dLng = (lng2 - lng1) * (Math.PI / 180.0);

        var rLat1 = lat1 * (Math.PI / 180.0);
        var rLat2 = lat2 * (Math.PI / 180.0);

        var sinDLat = Math.Sin(dLat / 2.0);
        var sinDLng = Math.Sin(dLng / 2.0);

        var a = (sinDLat * sinDLat) + Math.Cos(rLat1) * Math.Cos(rLat2) * (sinDLng * sinDLng);
        a = Math.Clamp(a, 0.0, 1.0);

        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));
        return EarthRadiusM * c;
    }

    public (double MinLat, double MaxLat, double MinLng, double MaxLng) CalculateBoundingBox(double lat, double lng, double radiusM)
    {
        if (radiusM <= 0)
        {
            return (lat, lat, lng, lng);
        }

        var dLat = (radiusM / EarthRadiusM) * (180.0 / Math.PI);
        var minLat = Math.Max(-90.0, lat - dLat);
        var maxLat = Math.Min(90.0, lat + dLat);

        if (Math.Abs(lat) >= 89.9)
        {
            return (minLat, maxLat, -180.0, 180.0);
        }

        var cosLat = Math.Cos(lat * Math.PI / 180.0);
        var dLng = (radiusM / (EarthRadiusM * cosLat)) * (180.0 / Math.PI);

        var minLng = lng - dLng;
        var maxLng = lng + dLng;

        return (minLat, maxLat, minLng, maxLng);
    }

    public IReadOnlyList<NearbyPoiResponse> FindNearbyPois(
        IReadOnlyList<Poi> pois,
        double clientLat,
        double clientLng,
        double radiusM,
        string? preferredLanguage = null)
    {
        if (pois == null || pois.Count == 0 || radiusM <= 0)
        {
            return Array.Empty<NearbyPoiResponse>();
        }

        var bbox = CalculateBoundingBox(clientLat, clientLng, radiusM);
        var result = new List<NearbyPoiResponse>();

        foreach (var poi in pois)
        {
            // Lọc sơ bộ bằng Bounding Box
            if (poi.Lat < bbox.MinLat || poi.Lat > bbox.MaxLat)
            {
                continue;
            }

            if (bbox.MinLng <= bbox.MaxLng)
            {
                if (poi.Lng < bbox.MinLng || poi.Lng > bbox.MaxLng)
                {
                    continue;
                }
            }

            var distanceM = CalculateHaversineDistanceM(clientLat, clientLng, poi.Lat, poi.Lng);
            if (distanceM > radiusM)
            {
                continue;
            }

            var roundedDistance = (int)Math.Round(distanceM, MidpointRounding.AwayFromZero);
            var withinTrigger = distanceM <= poi.TriggerRadiusM;

            var title = ResolvePoiTitle(poi, preferredLanguage);

            result.Add(new NearbyPoiResponse
            {
                Id = poi.Id,
                Title = title,
                Lat = poi.Lat,
                Lng = poi.Lng,
                DistanceM = roundedDistance,
                WithinTrigger = withinTrigger,
                TriggerRadiusM = poi.TriggerRadiusM,
                Priority = poi.Priority,
                ImageUrl = poi.ImageUrl
            });
        }

        // Sắp xếp: Priority giảm dần -> Khoảng cách tăng dần -> Id
        return result
            .OrderByDescending(p => p.Priority)
            .ThenBy(p => p.DistanceM)
            .ThenBy(p => p.Id)
            .ToList();
    }

    private static string ResolvePoiTitle(Poi poi, string? preferredLanguage)
    {
        if (poi.Translations == null || poi.Translations.Count == 0)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(preferredLanguage))
        {
            var matched = poi.Translations.FirstOrDefault(t =>
                string.Equals(t.LanguageCode, preferredLanguage, StringComparison.OrdinalIgnoreCase));
            if (matched != null && !string.IsNullOrWhiteSpace(matched.Title))
            {
                return matched.Title;
            }
        }

        var en = poi.Translations.FirstOrDefault(t => string.Equals(t.LanguageCode, "en", StringComparison.OrdinalIgnoreCase));
        if (en != null && !string.IsNullOrWhiteSpace(en.Title)) return en.Title;

        var vi = poi.Translations.FirstOrDefault(t => string.Equals(t.LanguageCode, "vi", StringComparison.OrdinalIgnoreCase));
        if (vi != null && !string.IsNullOrWhiteSpace(vi.Title)) return vi.Title;

        return poi.Translations.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Title))?.Title ?? string.Empty;
    }
}
