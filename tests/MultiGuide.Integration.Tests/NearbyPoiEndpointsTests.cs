using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Enums;

namespace MultiGuide.Integration.Tests;

public class NearbyPoiEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public NearbyPoiEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (HttpClient client, string dbName) CreateTestClient()
    {
        var dbName = $"NearbyPoiEndpoints_{Guid.NewGuid()}";
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", dbName);
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        return (client, dbName);
    }

    private async Task<(Guid areaId, string rawToken)> SeedAreaAndPassAsync(
        string dbName,
        string areaCode = "van-mieu",
        bool isPassExpired = false)
    {
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var isolatedContext = new MultiGuideDbContext(options);

        var area = new TourArea
        {
            Id = Guid.NewGuid(),
            Code = areaCode,
            Name = $"Khu {areaCode}",
            CenterLat = 21.0285,
            CenterLng = 105.8355,
            RadiusM = 300,
            PriceUsd = 5.0m,
            PassTtlHours = 24,
            IsActive = true
        };
        isolatedContext.TourAreas.Add(area);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            AreaId = area.Id,
            Amount = 5.0m,
            Currency = "USD",
            Provider = "mock",
            ProviderRef = "ref-1",
            IdempotencyKey = $"idem-{Guid.NewGuid()}",
            Status = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow
        };
        isolatedContext.Payments.Add(payment);

        var rawToken = $"token_{Guid.NewGuid():N}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        var tokenHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var pass = new AccessPass
        {
            Id = Guid.NewGuid(),
            AreaId = area.Id,
            PaymentId = payment.Id,
            TokenHash = tokenHash,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = isPassExpired ? DateTime.UtcNow.AddMinutes(-10) : DateTime.UtcNow.AddHours(24),
            Status = AccessPassStatus.Active
        };
        isolatedContext.AccessPasses.Add(pass);

        await isolatedContext.SaveChangesAsync();
        return (area.Id, rawToken);
    }

    private async Task SeedPoisAsync(string dbName, Guid areaId)
    {
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var isolatedContext = new MultiGuideDbContext(options);

        // POI 1: Cổng Văn Miếu - gần toạ độ khách (cách ~25m)
        var poi1 = new Poi
        {
            Id = Guid.NewGuid(),
            AreaId = areaId,
            Lat = 21.02860,
            Lng = 105.83552,
            TriggerRadiusM = 30,
            Priority = 50,
            ImageUrl = "https://example.com/poi1.jpg",
            IsActive = true,
            Translations =
            [
                new PoiTranslation { LanguageCode = "vi", Title = "Cổng Văn Miếu" },
                new PoiTranslation { LanguageCode = "en", Title = "Van Mieu Gate" }
            ]
        };

        // POI 2: Khuê Văn Các - cách toạ độ khách ~80m, Priority 90
        var poi2 = new Poi
        {
            Id = Guid.NewGuid(),
            AreaId = areaId,
            Lat = 21.02916,
            Lng = 105.83552,
            TriggerRadiusM = 30,
            Priority = 90,
            ImageUrl = "https://example.com/poi2.jpg",
            IsActive = true,
            Translations =
            [
                new PoiTranslation { LanguageCode = "vi", Title = "Khuê Văn Các" },
                new PoiTranslation { LanguageCode = "en", Title = "Khue Van Pavilion" }
            ]
        };

        // POI 3: Điểm xa - cách toạ độ khách ~800m (ngoài radius 100m/500m)
        var poi3 = new Poi
        {
            Id = Guid.NewGuid(),
            AreaId = areaId,
            Lat = 21.03600,
            Lng = 105.83552,
            TriggerRadiusM = 30,
            Priority = 20,
            IsActive = true,
            Translations =
            [
                new PoiTranslation { LanguageCode = "vi", Title = "Điểm ngoài tầm" }
            ]
        };

        // POI 4: Điểm bị vô hiệu hóa (IsActive = false)
        var poi4 = new Poi
        {
            Id = Guid.NewGuid(),
            AreaId = areaId,
            Lat = 21.02855,
            Lng = 105.83552,
            TriggerRadiusM = 30,
            Priority = 100,
            IsActive = false,
            Translations =
            [
                new PoiTranslation { LanguageCode = "vi", Title = "Điểm bảo trì" }
            ]
        };

        isolatedContext.Pois.AddRange(poi1, poi2, poi3, poi4);
        await isolatedContext.SaveChangesAsync();
    }

    [Theory]
    [InlineData("/pois/nearby?lat=21.02844&lng=105.83552")]
    [InlineData("/api/v1/pois/nearby?lat=21.02844&lng=105.83552")]
    public async Task GetNearby_KhongCoToken_TraVe401PassMissing(string url)
    {
        // Arrange
        var (client, _) = CreateTestClient();

        // Act
        var res = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.PassMissing, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetNearby_TokenHetHan_TraVe403PassExpired()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (_, expiredToken) = await SeedAreaAndPassAsync(dbName, isPassExpired: true);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/pois/nearby?lat=21.02844&lng=105.83552");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var res = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.PassExpired, problem.Extensions["code"]?.ToString());
    }

    [Theory]
    [InlineData("/pois/nearby?lng=105.83552")] // Thiếu lat
    [InlineData("/pois/nearby?lat=21.02844")]  // Thiếu lng
    [InlineData("/pois/nearby?lat=95.0&lng=105.83552")] // lat ngoài [-90, 90]
    [InlineData("/pois/nearby?lat=21.02844&lng=-190.0")] // lng ngoài [-180, 180]
    [InlineData("/pois/nearby?lat=21.02844&lng=105.83552&radius=0")] // radius <= 0
    [InlineData("/pois/nearby?lat=21.02844&lng=105.83552&radius=600")] // radius > 500
    public async Task GetNearby_ThamSoToaDoHoacRadiusKhongHopLe_TraVe422ValidationError(string url)
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (_, token) = await SeedAreaAndPassAsync(dbName);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.ValidationError, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetNearby_HopLe_TraVePoiTrongBanKinhVaSapXepChuan()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (areaId, token) = await SeedAreaAndPassAsync(dbName);
        await SeedPoisAsync(dbName, areaId);

        // Tọa độ khách: 21.02844, 105.83552 (bán kính mặc định 100m)
        using var request = new HttpRequestMessage(HttpMethod.Get, "/pois/nearby?lat=21.02844&lng=105.83552&lang=vi");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var pois = await res.Content.ReadFromJsonAsync<List<NearbyPoiResponse>>();

        Assert.NotNull(pois);
        // Chỉ có 2 POI trong bán kính 100m (poi1 và poi2), poi3 quá xa, poi4 inactive
        Assert.Equal(2, pois.Count);

        // Sắp xếp: poi2 có Priority 90 > poi1 Priority 50 => poi2 đứng đầu
        Assert.Equal("Khuê Văn Các", pois[0].Title);
        Assert.Equal(90, pois[0].Priority);
        Assert.False(pois[0].WithinTrigger); // cách ~80m > 30m

        Assert.Equal("Cổng Văn Miếu", pois[1].Title);
        Assert.Equal(50, pois[1].Priority);
        Assert.True(pois[1].WithinTrigger); // cách ~25m <= 30m
        Assert.Equal("https://example.com/poi1.jpg", pois[1].ImageUrl);
    }
}
