using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Enums;
using Xunit;

namespace MultiGuide.Integration.Tests;

public class AdminAnalyticsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestIssuer = "https://test-supabase.co/auth/v1";
    private const string TestAudience = "authenticated";
    private const string TestSecret = "my_super_secret_test_jwt_key_32_bytes_long_123";

    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public AdminAnalyticsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"AdminAnalytics_{Guid.NewGuid()}");

            builder.ConfigureServices(services =>
            {
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters.ValidateIssuer = true;
                    options.TokenValidationParameters.ValidIssuer = TestIssuer;
                    options.TokenValidationParameters.ValidateAudience = true;
                    options.TokenValidationParameters.ValidAudience = TestAudience;
                    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret));
                    options.MetadataAddress = null!;
                    options.Configuration = null;
                    options.ConfigurationManager = null!;
                });
            });
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static string GenerateJwt(string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestSecret);

        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("role", role),
            new(ClaimTypes.Role, role),
            new("app_metadata", $"{{\"role\":\"{role}\"}}")
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = TestIssuer,
            Audience = TestAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task GetDashboard_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/admin/analytics/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_WithNonAdminToken_Returns403Forbidden()
    {
        var token = GenerateJwt("user");
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/analytics/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_WithAdminToken_Returns200OkWithMetrics()
    {
        var token = GenerateJwt("admin");
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/analytics/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DashboardAnalyticsResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.TopPois);
        Assert.NotNull(result.LanguageDistribution);
        Assert.True(result.ActiveVisitorsCount >= 0);
    }

    [Fact]
    public async Task GetHourlyVisitors_WithoutToken_Returns401Unauthorized()
    {
        var response = await _client.GetAsync("/admin/analytics/hourly-visitors");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHourlyVisitors_WithNonAdminToken_Returns403Forbidden()
    {
        var token = GenerateJwt("user");
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/analytics/hourly-visitors");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public async Task GetHourlyVisitors_WithInvalidHours_Returns422UnprocessableEntity(int hours)
    {
        var token = GenerateJwt("admin");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/analytics/hourly-visitors?hours={hours}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetHourlyVisitors_WithAdminToken_ReturnsPassesGroupedByHour()
    {
        var now = DateTime.UtcNow;
        var areaId = Guid.NewGuid();
        var passRecentId = Guid.NewGuid();
        var passOneHourAgoId = Guid.NewGuid();
        var passOldId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MultiGuideDbContext>();
            db.TourAreas.Add(new TourArea
            {
                Id = areaId,
                Code = $"hourly-area-{areaId:N}"[..20],
                Name = "Văn Miếu Test",
                CenterLat = 21.027,
                CenterLng = 105.835,
                RadiusM = 500,
                PriceUsd = 2.00m,
                PassTtlHours = 24,
                IsActive = true
            });

            var payment1 = new Payment { Id = Guid.NewGuid(), AreaId = areaId, Amount = 2m, Currency = "USD", Provider = "mock", IdempotencyKey = Guid.NewGuid().ToString(), Status = PaymentStatus.Succeeded, CreatedAt = now.AddMinutes(-20) };
            var payment2 = new Payment { Id = Guid.NewGuid(), AreaId = areaId, Amount = 2m, Currency = "USD", Provider = "mock", IdempotencyKey = Guid.NewGuid().ToString(), Status = PaymentStatus.Succeeded, CreatedAt = now.AddMinutes(-90) };
            var payment3 = new Payment { Id = Guid.NewGuid(), AreaId = areaId, Amount = 2m, Currency = "USD", Provider = "mock", IdempotencyKey = Guid.NewGuid().ToString(), Status = PaymentStatus.Succeeded, CreatedAt = now.AddHours(-15) };
            db.Payments.AddRange(payment1, payment2, payment3);

            db.AccessPasses.AddRange(
                new AccessPass
                {
                    Id = passRecentId,
                    AreaId = areaId,
                    PaymentId = payment1.Id,
                    TokenHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                    IssuedAt = now.AddMinutes(-20),
                    ExpiresAt = now.AddHours(23),
                    Status = AccessPassStatus.Active
                },
                new AccessPass
                {
                    Id = passOneHourAgoId,
                    AreaId = areaId,
                    PaymentId = payment2.Id,
                    TokenHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                    IssuedAt = now.AddMinutes(-90),
                    ExpiresAt = now.AddHours(22),
                    Status = AccessPassStatus.Revoked
                },
                new AccessPass
                {
                    Id = passOldId,
                    AreaId = areaId,
                    PaymentId = payment3.Id,
                    TokenHash = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                    IssuedAt = now.AddHours(-15),
                    ExpiresAt = now.AddHours(9),
                    Status = AccessPassStatus.Active
                });

            await db.SaveChangesAsync();
        }

        var token = GenerateJwt("admin");
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/analytics/hourly-visitors?hours=12");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<HourlyVisitorsResponse>();
        Assert.NotNull(result);
        Assert.Equal(12, result.Hours);
        Assert.Equal(12, result.Buckets.Count);
        Assert.Equal(2, result.TotalVisitors);

        var bucket0 = result.Buckets.Single(b => b.HoursAgo == 0);
        Assert.Equal(1, bucket0.VisitorCount);
        Assert.Equal(passRecentId, bucket0.Visitors[0].PassId);
        Assert.Equal("active", bucket0.Visitors[0].Status);
        Assert.StartsWith("PASS-", bucket0.Visitors[0].PassLabel);

        var bucket1 = result.Buckets.Single(b => b.HoursAgo == 1);
        Assert.Equal(1, bucket1.VisitorCount);
        Assert.Equal(passOneHourAgoId, bucket1.Visitors[0].PassId);
        Assert.Equal("revoked", bucket1.Visitors[0].Status);
    }
}
