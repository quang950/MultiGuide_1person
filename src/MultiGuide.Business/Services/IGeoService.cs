using MultiGuide.Business.DTOs.Client;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Business.Services;

/// <summary>
/// Dịch vụ xử lý toạ độ địa lý, tính khoảng cách Haversine và tìm kiếm POI lân cận (US-05).
/// </summary>
public interface IGeoService
{
    /// <summary>
    /// Tính khoảng cách mặt cầu theo công thức Haversine giữa 2 toạ độ GPS (đơn vị: mét).
    /// </summary>
    double CalculateHaversineDistanceM(double lat1, double lng1, double lat2, double lng2);

    /// <summary>
    /// Tính toán hộp giới hạn toạ độ (Bounding Box) từ vị trí tâm và bán kính (mét).
    /// </summary>
    (double MinLat, double MaxLat, double MinLng, double MaxLng) CalculateBoundingBox(double lat, double lng, double radiusM);

    /// <summary>
    /// Lọc và tính khoảng cách, sắp xếp các POI trong bán kính tìm kiếm (ưu tiên Priority giảm dần, Distance tăng dần).
    /// </summary>
    IReadOnlyList<NearbyPoiResponse> FindNearbyPois(
        IReadOnlyList<Poi> pois,
        double clientLat,
        double clientLng,
        double radiusM,
        string? preferredLanguage = null);
}
