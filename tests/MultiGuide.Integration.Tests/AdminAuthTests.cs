using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MultiGuide.Domain.Constants;
using MultiGuide.Integration.Tests.TestControllers;

namespace MultiGuide.Integration.Tests;

public class AdminAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestIssuer = "https://test-supabase.co/auth/v1";
    private const string TestAudience = "authenticated";
    private const string TestSecret = "my_super_secret_test_jwt_key_32_bytes_long_123";

    private readonly HttpClient _client;

    public AdminAuthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"AdminAuth_{Guid.NewGuid()}");

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

                services.AddControllers()
                    .AddApplicationPart(typeof(AdminTestController).Assembly);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task WhenNoTokenProvided_Returns401WithAuthRequiredProblemDetails()
    {
        // Act - Thử nghiệm cả 2 route /admin/* và /api/v1/admin/*
        var response = await _client.GetAsync("/admin/admintest/ping");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(401, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.AuthRequired, content.GetProperty("code").GetString());
        Assert.Equal("Unauthorized", content.GetProperty("title").GetString());
    }

    [Fact]
    public async Task WhenInvalidOrMalformedToken_Returns401WithAuthRequiredProblemDetails()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/admintest/ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token-format");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(401, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.AuthRequired, content.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WhenExpiredToken_Returns401WithAuthRequiredProblemDetails()
    {
        // Arrange: token đã hết hạn 10 phút trước
        var expiredToken = GenerateJwt(
            claims: [new Claim("role", "admin")],
            expiresIn: TimeSpan.FromMinutes(-10));

        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/admintest/ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(401, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.AuthRequired, content.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WhenValidTokenWithoutAdminRole_Returns403WithForbiddenProblemDetails()
    {
        // Arrange: token hợp lệ của user thông thường (role = authenticated, không có role admin)
        var touristToken = GenerateJwt(
            claims:
            [
                new Claim("role", "authenticated"),
                new Claim("app_metadata", JsonSerializer.Serialize(new { provider = "email", role = "tourist" }))
            ]);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/admintest/ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", touristToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(403, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.Forbidden, content.GetProperty("code").GetString());
        Assert.Equal("Forbidden", content.GetProperty("title").GetString());
    }

    [Fact]
    public async Task WhenValidTokenWithSupabaseAppMetadataAdminRole_Returns200Ok()
    {
        // Arrange: token chuẩn Supabase với app_metadata: { "role": "admin" }
        var adminToken = GenerateJwt(
            claims:
            [
                new Claim("role", "authenticated"),
                new Claim("app_metadata", JsonSerializer.Serialize(new { provider = "email", role = "admin" }))
            ]);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/admin/admintest/ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pong", content.GetProperty("message").GetString());
        Assert.Equal("admin", content.GetProperty("role").GetString());
    }

    [Fact]
    public async Task WhenValidTokenWithDirectRoleAdminClaim_Returns200Ok()
    {
        // Arrange: token có claim role = "admin" trực tiếp
        var adminToken = GenerateJwt(
            claims:
            [
                new Claim("role", "admin")
            ]);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/admintest/ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("pong", content.GetProperty("message").GetString());
    }

    private static string GenerateJwt(
        IEnumerable<Claim> claims,
        TimeSpan? expiresIn = null,
        string secret = TestSecret)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secret);
        var now = DateTime.UtcNow;

        var expires = expiresIn.HasValue ? now.Add(expiresIn.Value) : now.AddHours(1);
        var notBefore = expiresIn.HasValue && expiresIn.Value < TimeSpan.Zero
            ? expires.AddMinutes(-30)
            : now.AddMinutes(-5);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = TestIssuer,
            Audience = TestAudience,
            NotBefore = notBefore,
            IssuedAt = notBefore,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
