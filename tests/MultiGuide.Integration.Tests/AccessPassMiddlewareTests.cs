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
using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Enums;
using MultiGuide.Integration.Tests.TestControllers;

namespace MultiGuide.Integration.Tests;

public class AccessPassMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AccessPassMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (HttpClient client, string dbName) CreateTestClient()
    {
        var dbName = $"AccessPassMiddleware_{Guid.NewGuid()}";
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", dbName);
            builder.ConfigureServices(services =>
            {
                services.AddControllers()
                    .AddApplicationPart(typeof(PassTestController).Assembly);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        return (client, dbName);
    }

    private async Task<(Guid passId, Guid areaId, string rawToken)> SeedPassAsync(
        string dbName,
        AccessPassStatus status = AccessPassStatus.Active,
        int expiresHoursFromNow = 24)
    {
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var isolatedContext = new MultiGuideDbContext(options);

        var area = new TourArea
        {
            Id = Guid.NewGuid(),
            Code = $"area-{Guid.NewGuid():N}",
            Name = "Khu kiểm thử",
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
            ProviderRef = "ref-123",
            IdempotencyKey = $"idem-{Guid.NewGuid()}",
            Status = PaymentStatus.Succeeded,
            CreatedAt = DateTime.UtcNow
        };
        isolatedContext.Payments.Add(payment);

        var rawToken = $"token_{Guid.NewGuid():N}_{Guid.NewGuid():N}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        var tokenHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var passId = Guid.NewGuid();
        var pass = new AccessPass
        {
            Id = passId,
            AreaId = area.Id,
            PaymentId = payment.Id,
            TokenHash = tokenHash,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(expiresHoursFromNow),
            Status = status
        };
        isolatedContext.AccessPasses.Add(pass);

        await isolatedContext.SaveChangesAsync();
        return (passId, area.Id, rawToken);
    }

    [Fact]
    public async Task GetProtected_KhongCoHeaderAuthorization_TraVe401PassMissing()
    {
        // Arrange
        var (client, _) = CreateTestClient();

        // Act
        var res = await client.GetAsync("/test/pass-protected");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);

        var body = await res.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(problem);
        Assert.Equal(401, problem.Status);
        Assert.Equal(ErrorCodes.PassMissing, problem.Extensions["code"]?.ToString());
    }

    [Theory]
    [InlineData("Basic abcxyz")]
    [InlineData("Token abcxyz")]
    [InlineData("Bearer ")]
    [InlineData("Bearer    ")]
    public async Task GetProtected_HeaderAuthorizationSaiDinhDang_TraVe401PassMissing(string authHeader)
    {
        // Arrange
        var (client, _) = CreateTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/pass-protected");
        request.Headers.TryAddWithoutValidation("Authorization", authHeader);

        // Act
        var res = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);

        var body = await res.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(problem);
        Assert.Equal(401, problem.Status);
        Assert.Equal(ErrorCodes.PassMissing, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetProtected_TokenKhongTonTai_TraVe403PassInvalid()
    {
        // Arrange
        var (client, _) = CreateTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/pass-protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "non-existent-token-12345");

        // Act
        var res = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);

        var body = await res.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(problem);
        Assert.Equal(403, problem.Status);
        Assert.Equal(ErrorCodes.PassInvalid, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetProtected_TokenBiRevoked_TraVe403PassInvalid()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (_, _, rawToken) = await SeedPassAsync(dbName, status: AccessPassStatus.Revoked);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/pass-protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);

        // Act
        var res = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);

        var body = await res.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(problem);
        Assert.Equal(403, problem.Status);
        Assert.Equal(ErrorCodes.PassInvalid, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetProtected_TokenDaHetHan_TraVe403PassExpired()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (_, _, rawToken) = await SeedPassAsync(dbName, status: AccessPassStatus.Active, expiresHoursFromNow: -1);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/pass-protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);

        // Act
        var res = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal("application/problem+json", res.Content.Headers.ContentType?.MediaType);

        var body = await res.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(problem);
        Assert.Equal(403, problem.Status);
        Assert.Equal(ErrorCodes.PassExpired, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task GetProtected_TokenHopLe_TraVe200OkVaGanPassContext()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        var (passId, areaId, rawToken) = await SeedPassAsync(dbName, status: AccessPassStatus.Active, expiresHoursFromNow: 24);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/test/pass-protected");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);

        // Act
        var res = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var responseData = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(passId.ToString(), responseData.GetProperty("pass_id").GetString());
        Assert.Equal(areaId.ToString(), responseData.GetProperty("area_id").GetString());
    }
}
