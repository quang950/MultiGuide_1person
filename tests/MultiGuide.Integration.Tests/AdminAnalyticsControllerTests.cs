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
using Xunit;

namespace MultiGuide.Integration.Tests;

public class AdminAnalyticsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestIssuer = "https://test-supabase.co/auth/v1";
    private const string TestAudience = "authenticated";
    private const string TestSecret = "my_super_secret_test_jwt_key_32_bytes_long_123";

    private readonly HttpClient _client;

    public AdminAnalyticsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
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
        }).CreateClient(new WebApplicationFactoryClientOptions
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
}
