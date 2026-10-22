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

public class PassCurrentEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PassCurrentEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (HttpClient client, string dbName) CreateTestClient()
    {
        var dbName = $"PassCurrentEndpoints_{Guid.NewGuid()}";
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
        bool isPassExpired = false)
    {
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var isolatedContext = new MultiGuideDbContext(options);

        var area = new TourArea
        {
            Id = Guid.NewGuid(),
            Code = "van-mieu",
            Name = "Khu Văn Miếu",
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

    [Theory]
    [InlineData("/passes/current")]
    [InlineData("/api/v1/passes/current")]
    public async Task GetCurrent_KhongCoToken_TraVe401PassMissing(string url)
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
    public async Task GetCurrent_TokenHetHan_TraVe403PassExpired()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (_, _, token) = await SeedAreaAndPassAsync(dbName, isPassExpired: true);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.GetAsync("/passes/current");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(ErrorCodes.PassExpired, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetCurrent_TokenHopLe_TraVe200VaThongTinPass()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (passId, areaId, token) = await SeedAreaAndPassAsync(dbName);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.GetAsync("/passes/current");

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var pass = await res.Content.ReadFromJsonAsync<CurrentPassResponse>();
        Assert.NotNull(pass);

        Assert.Equal(passId, pass.Id);
        Assert.Equal(areaId, pass.AreaId);
        Assert.Equal("active", pass.Status);
        Assert.True(pass.RemainingSeconds > 0);
        Assert.True(pass.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task Heartbeat_KhongCoToken_TraVe401()
    {
        // Arrange
        var (client, _) = CreateTestClient();

        // Act
        var res = await client.PostAsync("/passes/heartbeat", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Heartbeat_TokenHopLe_TraVe200VaAlive()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (_, _, token) = await SeedAreaAndPassAsync(dbName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var res = await client.PostAsync("/passes/heartbeat", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
