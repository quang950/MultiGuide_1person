using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
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

public class PlayLogsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PlayLogsEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (HttpClient client, string dbName) CreateTestClient()
    {
        var dbName = $"PlayLogsEndpoints_{Guid.NewGuid()}";
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

    private async Task<(Guid passId, Guid areaId, string rawToken)> SeedAreaAndPassAsync(
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
        return (pass.Id, area.Id, rawToken);
    }

    private async Task<Poi> SeedPoiAsync(string dbName, Guid areaId, bool isActive = true)
    {
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var isolatedContext = new MultiGuideDbContext(options);

        var poi = new Poi
        {
            Id = Guid.NewGuid(),
            AreaId = areaId,
            Lat = 21.0286,
            Lng = 105.8355,
            TriggerRadiusM = 30,
            Priority = 50,
            IsActive = isActive
        };

        isolatedContext.Pois.Add(poi);
        await isolatedContext.SaveChangesAsync();
        return poi;
    }

    [Theory]
    [InlineData("/play-logs")]
    [InlineData("/api/v1/play-logs")]
    public async Task CreatePlayLog_KhongCoToken_TraVe401PassMissing(string url)
    {
        // Arrange
        var (client, _) = CreateTestClient();
        var req = new CreatePlayLogRequest
        {
            PoiId = Guid.NewGuid(),
            LanguageCode = "vi",
            ListenedSec = 60,
            Completed = true
        };

        // Act
        var res = await client.PostAsJsonAsync(url, req);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.PassMissing, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task CreatePlayLog_TokenHetHan_TraVe403PassExpired()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (_, areaId, token) = await SeedAreaAndPassAsync(dbName, isPassExpired: true);
        var poi = await SeedPoiAsync(dbName, areaId);

        var req = new CreatePlayLogRequest
        {
            PoiId = poi.Id,
            LanguageCode = "vi",
            ListenedSec = 60,
            Completed = true
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.PostAsJsonAsync("/play-logs", req);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.PassExpired, problem.Extensions["code"]?.ToString());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(25000)]
    public async Task CreatePlayLog_ListenedSecNgoaiPhamVi_TraVe422ValidationError(int listenedSec)
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (_, areaId, token) = await SeedAreaAndPassAsync(dbName);
        var poi = await SeedPoiAsync(dbName, areaId);

        var req = new CreatePlayLogRequest
        {
            PoiId = poi.Id,
            LanguageCode = "vi",
            ListenedSec = listenedSec,
            Completed = false
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.PostAsJsonAsync("/play-logs", req);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.ValidationError, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task CreatePlayLog_PoiKhongTonTai_TraVe404PoiContentNotFound()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (_, _, token) = await SeedAreaAndPassAsync(dbName);

        var req = new CreatePlayLogRequest
        {
            PoiId = Guid.NewGuid(),
            LanguageCode = "vi",
            ListenedSec = 60,
            Completed = true
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.PostAsJsonAsync("/play-logs", req);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.PoiContentNotFound, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task CreatePlayLog_PoiKhacKhuVucCuaPass_TraVe403PassAreaMismatch()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (_, _, token) = await SeedAreaAndPassAsync(dbName, areaCode: "van-mieu");

        // Tạo area khác và POI thuộc area đó
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using (var isolatedContext = new MultiGuideDbContext(options))
        {
            var otherArea = new TourArea
            {
                Id = Guid.NewGuid(),
                Code = "hoang-thanh",
                Name = "Khu Hoàng Thành",
                CenterLat = 21.033,
                CenterLng = 105.84,
                RadiusM = 400,
                PriceUsd = 6.0m,
                PassTtlHours = 24,
                IsActive = true
            };
            isolatedContext.TourAreas.Add(otherArea);
            await isolatedContext.SaveChangesAsync();

            var poi = await SeedPoiAsync(dbName, otherArea.Id);

            var req = new CreatePlayLogRequest
            {
                PoiId = poi.Id,
                LanguageCode = "vi",
                ListenedSec = 120,
                Completed = true
            };

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var res = await client.PostAsJsonAsync("/play-logs", req);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
            var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal(ErrorCodes.PassAreaMismatch, problem.Extensions["code"]?.ToString());
        }
    }

    [Fact]
    public async Task CreatePlayLog_HopLe_TraVe201VaLuuDB()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (passId, areaId, token) = await SeedAreaAndPassAsync(dbName);
        var poi = await SeedPoiAsync(dbName, areaId);

        var req = new CreatePlayLogRequest
        {
            PoiId = poi.Id,
            LanguageCode = "VI",
            ListenedSec = 150,
            Completed = true
        };

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.PostAsJsonAsync("/play-logs", req);

        // Assert
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var playLogRes = await res.Content.ReadFromJsonAsync<PlayLogResponse>();
        Assert.NotNull(playLogRes);

        Assert.Equal(passId, playLogRes.PassId);
        Assert.Equal(poi.Id, playLogRes.PoiId);
        Assert.Equal("vi", playLogRes.LanguageCode);
        Assert.Equal(150, playLogRes.ListenedSec);
        Assert.True(playLogRes.Completed);

        // Kiểm tra trong CSDL In-Memory
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var isolatedContext = new MultiGuideDbContext(options);
        var savedLog = await isolatedContext.PlayLogs.FirstOrDefaultAsync(p => p.Id == playLogRes.Id);
        Assert.NotNull(savedLog);
        Assert.Equal(passId, savedLog.PassId);
        Assert.Equal(poi.Id, savedLog.PoiId);
        Assert.Equal(150, savedLog.ListenedSec);
        Assert.True(savedLog.Completed);
    }
}
