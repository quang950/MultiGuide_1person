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
using MultiGuide.Business.Storage;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace MultiGuide.Integration.Tests;

public class PoiDetailEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PoiDetailEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    public class FakeFileStorage : IFileStorage
    {
        public Task<string> UploadAsync(Stream content, string path, string contentType, CancellationToken cancellationToken = default) =>
            Task.FromResult(path);

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<string> CreateSignedUrlAsync(string path, TimeSpan expiresIn, CancellationToken cancellationToken = default) =>
            Task.FromResult($"https://fake-storage.local/{path}?token=mock-signed-1h");
    }

    private (HttpClient client, string dbName) CreateTestClient()
    {
        var dbName = $"PoiDetailEndpoints_{Guid.NewGuid()}";
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", dbName);
            builder.ConfigureServices(services =>
            {
                var storageDescriptors = services.Where(d => d.ServiceType == typeof(IFileStorage)).ToList();
                foreach (var d in storageDescriptors)
                {
                    services.Remove(d);
                }
                services.AddSingleton<IFileStorage, FakeFileStorage>();
            });
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

    private async Task<Poi> SeedPoiAsync(
        string dbName,
        Guid areaId,
        bool isActive = true,
        List<PoiTranslation>? translations = null)
    {
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var isolatedContext = new MultiGuideDbContext(options);

        var poi = new Poi
        {
            Id = Guid.NewGuid(),
            AreaId = areaId,
            Lat = 21.02860,
            Lng = 105.83552,
            TriggerRadiusM = 30,
            Priority = 50,
            ImageUrl = "https://example.com/poi.jpg",
            IsActive = isActive,
            Translations = translations ??
            [
                new PoiTranslation
                {
                    LanguageCode = "vi",
                    Title = "Khuê Văn Các",
                    Description = "Biểu tượng văn hóa Thủ đô Hà Nội.",
                    AudioPath = "audio/khue_van_cac_vi.mp3",
                    AudioDurationSec = 120
                },
                new PoiTranslation
                {
                    LanguageCode = "en",
                    Title = "Khue Van Pavilion",
                    Description = "The cultural symbol of Hanoi capital.",
                    AudioPath = "audio/khue_van_cac_en.mp3",
                    AudioDurationSec = 115
                }
            ]
        };

        isolatedContext.Pois.Add(poi);
        await isolatedContext.SaveChangesAsync();
        return poi;
    }

    [Theory]
    [InlineData("/pois")]
    [InlineData("/api/v1/pois")]
    public async Task GetById_KhongCoToken_TraVe401PassMissing(string baseUrl)
    {
        // Arrange
        var (client, _) = CreateTestClient();
        var poiId = Guid.NewGuid();

        // Act
        var res = await client.GetAsync($"{baseUrl}/{poiId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.PassMissing, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetById_TokenHetHan_TraVe403PassExpired()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (areaId, token) = await SeedAreaAndPassAsync(dbName, isPassExpired: true);
        var poi = await SeedPoiAsync(dbName, areaId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.GetAsync($"/pois/{poi.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.PassExpired, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetById_PoiKhongTonTai_TraVe404PoiContentNotFound()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (_, token) = await SeedAreaAndPassAsync(dbName);
        var nonExistentPoiId = Guid.NewGuid();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.GetAsync($"/pois/{nonExistentPoiId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.PoiContentNotFound, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetById_PoiInactive_TraVe404PoiContentNotFound()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (areaId, token) = await SeedAreaAndPassAsync(dbName);
        var poi = await SeedPoiAsync(dbName, areaId, isActive: false);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.GetAsync($"/pois/{poi.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.PoiContentNotFound, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetById_PassKhongDungKhu_TraVe403PassAreaMismatch()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        // Pass thuộc area A (van-mieu)
        var (_, token) = await SeedAreaAndPassAsync(dbName, areaCode: "van-mieu");

        // Tạo area B (hoang-thanh) và POI thuộc area B
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using (var isolatedContext = new MultiGuideDbContext(options))
        {
            var areaB = new TourArea
            {
                Id = Guid.NewGuid(),
                Code = "hoang-thanh",
                Name = "Hoàng Thành Thăng Long",
                CenterLat = 21.0333,
                CenterLng = 105.8400,
                RadiusM = 400,
                PriceUsd = 6.0m,
                PassTtlHours = 24,
                IsActive = true
            };
            isolatedContext.TourAreas.Add(areaB);
            await isolatedContext.SaveChangesAsync();

            var poiB = await SeedPoiAsync(dbName, areaB.Id);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act: lấy POI khu B bằng pass khu A
            var res = await client.GetAsync($"/pois/{poiB.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
            var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal(ErrorCodes.PassAreaMismatch, problem.Extensions["code"]?.ToString());
        }
    }

    [Fact]
    public async Task GetById_DungNgonNguYeuCau_TraVe200VaFallbackNull()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (areaId, token) = await SeedAreaAndPassAsync(dbName);
        var poi = await SeedPoiAsync(dbName, areaId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.GetAsync($"/pois/{poi.Id}?lang=vi");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var detail = await res.Content.ReadFromJsonAsync<PoiDetailResponse>();
        Assert.NotNull(detail);

        Assert.Equal(poi.Id, detail.Id);
        Assert.Equal(areaId, detail.AreaId);
        Assert.Equal("Khuê Văn Các", detail.Title);
        Assert.Equal("vi", detail.Language);
        Assert.Null(detail.FallbackLanguage);
        Assert.Equal("https://fake-storage.local/audio/khue_van_cac_vi.mp3?token=mock-signed-1h", detail.AudioUrl);
        Assert.Equal(120, detail.AudioDurationSec);
    }

    [Fact]
    public async Task GetById_LangKhongCo_FallbackSangEnglish_TraVe200VaFallbackLanguageEn()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (areaId, token) = await SeedAreaAndPassAsync(dbName);
        var poi = await SeedPoiAsync(dbName, areaId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act: yêu cầu tiếng Nhật ja (không có, có en và vi)
        var res = await client.GetAsync($"/pois/{poi.Id}?lang=ja");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var detail = await res.Content.ReadFromJsonAsync<PoiDetailResponse>();
        Assert.NotNull(detail);

        Assert.Equal("Khue Van Pavilion", detail.Title);
        Assert.Equal("en", detail.Language);
        Assert.Equal("en", detail.FallbackLanguage);
    }

    [Fact]
    public async Task GetById_LangKhongCoVaKhongCoEn_FallbackSangVietnamese_TraVe200VaFallbackLanguageVi()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (areaId, token) = await SeedAreaAndPassAsync(dbName);
        var poi = await SeedPoiAsync(dbName, areaId, translations:
        [
            new PoiTranslation
            {
                LanguageCode = "vi",
                Title = "Bia Tiến Sĩ",
                Description = "Di sản tư liệu thế giới UNESCO."
            }
        ]);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act: yêu cầu tiếng Pháp fr (không có fr, không có en, chỉ có vi)
        var res = await client.GetAsync($"/pois/{poi.Id}?lang=fr");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var detail = await res.Content.ReadFromJsonAsync<PoiDetailResponse>();
        Assert.NotNull(detail);

        Assert.Equal("Bia Tiến Sĩ", detail.Title);
        Assert.Equal("vi", detail.Language);
        Assert.Equal("vi", detail.FallbackLanguage);
    }
}
