using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Integration.Tests;

public class AreasControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _dbName;

    public AreasControllerTests(WebApplicationFactory<Program> factory)
    {
        _dbName = $"AreasIntegration_{Guid.NewGuid()}";
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", _dbName);
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private async Task SeedDataAsync(Func<MultiGuideDbContext, Task> seedAction)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiGuideDbContext>();
        await seedAction(context);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByCode_KhuTonTaiVaCoBanDich_TraVe200VaDayDuThongTinVaNgonNgu()
    {
        // Arrange
        var areaId = Guid.NewGuid();
        var poiId = Guid.NewGuid();

        await SeedDataAsync(async context =>
        {
            var area = new TourArea
            {
                Id = areaId,
                Code = "van-mieu",
                Name = "Văn Miếu - Quốc Tử Giám",
                CenterLat = 21.0285,
                CenterLng = 105.8355,
                RadiusM = 300,
                PriceUsd = 5.0m,
                PassTtlHours = 24,
                IsActive = true
            };
            context.TourAreas.Add(area);

            var viLang = new Language { Code = "vi", Name = "Tiếng Việt", IsActive = true };
            var enLang = new Language { Code = "en", Name = "English", IsActive = true };
            var jaLang = new Language { Code = "ja", Name = "日本語", IsActive = true };
            context.Languages.AddRange(viLang, enLang, jaLang);

            var poi = new Poi
            {
                Id = poiId,
                AreaId = areaId,
                Lat = 21.0285,
                Lng = 105.8355,
                TriggerRadiusM = 30,
                IsActive = true
            };
            context.Pois.Add(poi);

            context.PoiTranslations.AddRange(
                new PoiTranslation { PoiId = poiId, LanguageCode = "vi", Title = "Cổng Văn Miếu", Description = "Mô tả vi" },
                new PoiTranslation { PoiId = poiId, LanguageCode = "en", Title = "Van Mieu Gate", Description = "Desc en" },
                new PoiTranslation { PoiId = poiId, LanguageCode = "ja", Title = "文廟門", Description = "Desc ja" }
            );
        });

        // Act - Thử nghiệm cả 2 route: /areas/{code} và /api/v1/areas/{code}
        var response1 = await _client.GetAsync("/areas/van-mieu");
        var response2 = await _client.GetAsync("/api/v1/areas/van-mieu");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var data = await response1.Content.ReadFromJsonAsync<AreaDetailResponse>();
        Assert.NotNull(data);
        Assert.Equal(areaId, data.Id);
        Assert.Equal("van-mieu", data.Code);
        Assert.Equal("Văn Miếu - Quốc Tử Giám", data.Name);
        Assert.Equal(21.0285, data.CenterLat);
        Assert.Equal(105.8355, data.CenterLng);
        Assert.Equal(300, data.RadiusM);
        Assert.Equal(5.0m, data.PriceUsd);
        Assert.Equal(24, data.PassTtlHours);

        Assert.Equal(3, data.SupportedLanguages.Count);
        var langCodes = data.SupportedLanguages.Select(l => l.Code).ToList();
        Assert.Contains("vi", langCodes);
        Assert.Contains("en", langCodes);
        Assert.Contains("ja", langCodes);
    }

    [Fact]
    public async Task GetByCode_KhuKhongTonTai_TraVe404ProblemDetails_CodeAreaNotFound()
    {
        // Act
        var response = await _client.GetAsync("/areas/khu-khong-co-that");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("code", out var codeElement));
        Assert.Equal(ErrorCodes.AreaNotFound, codeElement.GetString());
    }

    [Fact]
    public async Task GetByCode_KhuBiTatIsActiveFalse_TraVe404ProblemDetails_CodeAreaNotFound()
    {
        // Arrange
        await SeedDataAsync(async context =>
        {
            var area = new TourArea
            {
                Id = Guid.NewGuid(),
                Code = "khu-tam-dong",
                Name = "Khu Tạm Đóng",
                IsActive = false
            };
            context.TourAreas.Add(area);
        });

        // Act
        var response = await _client.GetAsync("/areas/khu-tam-dong");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("code", out var codeElement));
        Assert.Equal(ErrorCodes.AreaNotFound, codeElement.GetString());
    }

    [Fact]
    public async Task GetByCode_PoiHoacNgonNguInactive_KhongXuatHienTrongSupportedLanguages()
    {
        // Arrange
        var areaId = Guid.NewGuid();
        var activePoiId = Guid.NewGuid();
        var inactivePoiId = Guid.NewGuid();

        await SeedDataAsync(async context =>
        {
            var area = new TourArea
            {
                Id = areaId,
                Code = "khu-loc-lang",
                Name = "Khu Lọc Ngôn Ngữ",
                IsActive = true
            };
            context.TourAreas.Add(area);

            var viLang = new Language { Code = "vi", Name = "Tiếng Việt", IsActive = true };
            var frLang = new Language { Code = "fr", Name = "Français", IsActive = true };
            var deLang = new Language { Code = "de", Name = "Deutsch", IsActive = false }; // inactive language
            context.Languages.AddRange(viLang, frLang, deLang);

            var activePoi = new Poi
            {
                Id = activePoiId,
                AreaId = areaId,
                IsActive = true
            };
            var inactivePoi = new Poi
            {
                Id = inactivePoiId,
                AreaId = areaId,
                IsActive = false // inactive POI
            };
            context.Pois.AddRange(activePoi, inactivePoi);

            context.PoiTranslations.AddRange(
                // active poi + active lang -> OK
                new PoiTranslation { PoiId = activePoiId, LanguageCode = "vi", Title = "Điểm A", Description = "Mô tả" },
                // active poi + inactive lang -> KHÔNG ĐƯỢC
                new PoiTranslation { PoiId = activePoiId, LanguageCode = "de", Title = "Punkt A", Description = "Desc" },
                // inactive poi + active lang -> KHÔNG ĐƯỢC
                new PoiTranslation { PoiId = inactivePoiId, LanguageCode = "fr", Title = "Point B", Description = "Desc" }
            );
        });

        // Act
        var response = await _client.GetAsync("/areas/khu-loc-lang");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<AreaDetailResponse>();
        Assert.NotNull(data);
        Assert.Single(data.SupportedLanguages);
        Assert.Equal("vi", data.SupportedLanguages[0].Code);
    }
}
