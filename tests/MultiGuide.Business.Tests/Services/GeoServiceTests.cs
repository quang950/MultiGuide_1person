using MultiGuide.Business.Services;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Business.Tests.Services;

public class GeoServiceTests
{
    private readonly GeoService _geoService = new();

    [Fact]
    public void CalculateHaversineDistanceM_HaiToaDoTrungNhau_TraVe0Met()
    {
        var distance = _geoService.CalculateHaversineDistanceM(21.0285, 105.8355, 21.0285, 105.8355);
        Assert.Equal(0.0, distance);
    }

    [Fact]
    public void CalculateHaversineDistanceM_HaiDiemThucTe_VanMieu_TraVeKhoangCachChinhXac()
    {
        // Cổng Văn Miếu: 21.02844, 105.83552
        // Khuê Văn Các: 21.02947, 105.83581
        // Khoảng cách thực tế xấp xỉ 118 - 120 mét
        var distance = _geoService.CalculateHaversineDistanceM(21.02844, 105.83552, 21.02947, 105.83581);

        Assert.InRange(distance, 115.0, 125.0);
    }

    [Fact]
    public void CalculateHaversineDistanceM_DiemCucBacVaCucNam_TinhDungNuaChuViTraiDat()
    {
        // Cực Bắc (90, 0) đến Cực Nam (-90, 0) -> Bán nguyệt pi * R xấp xỉ 20,015 km
        var distance = _geoService.CalculateHaversineDistanceM(90.0, 0.0, -90.0, 0.0);
        Assert.InRange(distance, 20_000_000, 20_050_000);
    }

    [Fact]
    public void CalculateBoundingBox_BanKinh500m_MinBeHonMax()
    {
        var bbox = _geoService.CalculateBoundingBox(21.0285, 105.8355, 500);

        Assert.True(bbox.MinLat < bbox.MaxLat);
        Assert.True(bbox.MinLng < bbox.MaxLng);
        Assert.True(bbox.MinLat < 21.0285 && 21.0285 < bbox.MaxLat);
        Assert.True(bbox.MinLng < 105.8355 && 105.8355 < bbox.MaxLng);
    }

    [Fact]
    public void CalculateBoundingBox_BanKinh0_TraVeDiemGoc()
    {
        var bbox = _geoService.CalculateBoundingBox(21.0285, 105.8355, 0);

        Assert.Equal(21.0285, bbox.MinLat);
        Assert.Equal(21.0285, bbox.MaxLat);
        Assert.Equal(105.8355, bbox.MinLng);
        Assert.Equal(105.8355, bbox.MaxLng);
    }

    [Fact]
    public void FindNearbyPois_DanhSachRongHoacRadius0_TraVeRong()
    {
        var res1 = _geoService.FindNearbyPois([], 21.0285, 105.8355, 100);
        Assert.Empty(res1);

        var poi = new Poi { Id = Guid.NewGuid(), Lat = 21.0285, Lng = 105.8355 };
        var res2 = _geoService.FindNearbyPois([poi], 21.0285, 105.8355, 0);
        Assert.Empty(res2);
    }

    [Fact]
    public void FindNearbyPois_DiemNgoaiBanKinh_BiLoaiBo()
    {
        // POI ở cách ~1.1 km
        var farPoi = new Poi
        {
            Id = Guid.NewGuid(),
            Lat = 21.0385,
            Lng = 105.8355,
            TriggerRadiusM = 30,
            Priority = 10,
            Translations = [new PoiTranslation { LanguageCode = "vi", Title = "Điểm xa" }]
        };

        var res = _geoService.FindNearbyPois([farPoi], 21.0285, 105.8355, 500);
        Assert.Empty(res);
    }

    [Fact]
    public void FindNearbyPois_DiemTrongBanKinh_TraVeDungKhoangCachVaWithinTrigger()
    {
        // Khách ở 21.02844, 105.83552
        // POI 1: Cách ~25m (trong trigger_radius_m = 30) -> within_trigger = true
        var poiNear = new Poi
        {
            Id = Guid.NewGuid(),
            Lat = 21.02860,
            Lng = 105.83552,
            TriggerRadiusM = 30,
            Priority = 50,
            Translations = [new PoiTranslation { LanguageCode = "vi", Title = "Cổng Văn Miếu" }]
        };

        // POI 2: Cách ~80m (ngoài trigger_radius_m = 30, nhưng trong radius = 100) -> within_trigger = false
        var poiMid = new Poi
        {
            Id = Guid.NewGuid(),
            Lat = 21.02916,
            Lng = 105.83552,
            TriggerRadiusM = 30,
            Priority = 80,
            Translations = [new PoiTranslation { LanguageCode = "vi", Title = "Khuê Văn Các" }]
        };

        var res = _geoService.FindNearbyPois([poiNear, poiMid], 21.02844, 105.83552, 100);

        Assert.Equal(2, res.Count);

        var pNear = res.First(p => p.Id == poiNear.Id);
        Assert.True(pNear.WithinTrigger);
        Assert.True(pNear.DistanceM <= 30);

        var pMid = res.First(p => p.Id == poiMid.Id);
        Assert.False(pMid.WithinTrigger);
        Assert.True(pMid.DistanceM > 30 && pMid.DistanceM <= 100);
    }

    [Fact]
    public void FindNearbyPois_SapXepTheoPriorityGiamDanSauDoDistanceTangDan()
    {
        var poiPrio10_Dist20 = new Poi
        {
            Id = Guid.NewGuid(),
            Lat = 21.02855,
            Lng = 105.83552,
            Priority = 10
        };

        var poiPrio80_Dist60 = new Poi
        {
            Id = Guid.NewGuid(),
            Lat = 21.02898,
            Lng = 105.83552,
            Priority = 80
        };

        var poiPrio80_Dist30 = new Poi
        {
            Id = Guid.NewGuid(),
            Lat = 21.02871,
            Lng = 105.83552,
            Priority = 80
        };

        var res = _geoService.FindNearbyPois(
            [poiPrio10_Dist20, poiPrio80_Dist60, poiPrio80_Dist30],
            21.02844,
            105.83552,
            200);

        Assert.Equal(3, res.Count);
        // Thứ tự mong muốn:
        // 1. poiPrio80_Dist30 (Priority 80, Distance ~30m)
        // 2. poiPrio80_Dist60 (Priority 80, Distance ~60m)
        // 3. poiPrio10_Dist20 (Priority 10, Distance ~12m)
        Assert.Equal(poiPrio80_Dist30.Id, res[0].Id);
        Assert.Equal(poiPrio80_Dist60.Id, res[1].Id);
        Assert.Equal(poiPrio10_Dist20.Id, res[2].Id);
    }

    [Fact]
    public void FindNearbyPois_NgonNguUuTienVaFallback()
    {
        var poi = new Poi
        {
            Id = Guid.NewGuid(),
            Lat = 21.0285,
            Lng = 105.8355,
            Translations =
            [
                new PoiTranslation { LanguageCode = "vi", Title = "Khuê Văn Các" },
                new PoiTranslation { LanguageCode = "en", Title = "Khue Van Pavilion" },
                new PoiTranslation { LanguageCode = "ja", Title = "奎文閣" }
            ]
        };

        // Yêu cầu tiếng Nhật
        var resJa = _geoService.FindNearbyPois([poi], 21.0285, 105.8355, 100, preferredLanguage: "ja");
        Assert.Equal("奎文閣", resJa[0].Title);

        // Yêu cầu tiếng Pháp (không có) -> fallback sang en
        var resFr = _geoService.FindNearbyPois([poi], 21.0285, 105.8355, 100, preferredLanguage: "fr");
        Assert.Equal("Khue Van Pavilion", resFr[0].Title);

        // Không truyền ngôn ngữ -> fallback sang en
        var resDefault = _geoService.FindNearbyPois([poi], 21.0285, 105.8355, 100);
        Assert.Equal("Khue Van Pavilion", resDefault[0].Title);
    }
}
