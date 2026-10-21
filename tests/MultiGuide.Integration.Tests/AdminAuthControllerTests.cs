using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;
using Xunit;

namespace MultiGuide.Integration.Tests;

public class AdminAuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminAuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private class FakeAdminAuthService : IAdminAuthService
    {
        public Func<string, string, Task<AdminLoginResponse>>? LoginHandler { get; set; }
        public Func<string, Task<AdminRefreshTokenResponse>>? RefreshHandler { get; set; }

        public Task<AdminLoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (LoginHandler != null) return LoginHandler(email, password);
            return Task.FromResult(new AdminLoginResponse("default-token", email, "admin", "default-refresh", 3600));
        }

        public Task<AdminRefreshTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            if (RefreshHandler != null) return RefreshHandler(refreshToken);
            return Task.FromResult(new AdminRefreshTokenResponse("default-new-token", "default-new-refresh", 3600, "admin"));
        }
    }

    private (HttpClient Client, FakeAdminAuthService FakeService) CreateClientWithFakeService()
    {
        var fakeService = new FakeAdminAuthService();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"AdminAuthCtrl_{Guid.NewGuid()}");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IAdminAuthService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped<IAdminAuthService>(_ => fakeService);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        return (client, fakeService);
    }

    [Fact]
    public async Task Refresh_WhenValidRefreshToken_Returns200WithNewTokens()
    {
        // Arrange
        var (client, fakeService) = CreateClientWithFakeService();
        var expectedResponse = new AdminRefreshTokenResponse("new-access-token-123", "new-refresh-token-456", 3600, "admin");

        fakeService.RefreshHandler = token =>
        {
            if (token == "valid-refresh-token") return Task.FromResult(expectedResponse);
            throw new UnauthorizedException(ErrorCodes.AuthRequired, "Invalid");
        };

        var request = new AdminRefreshTokenRequest("valid-refresh-token");

        // Act
        var response = await client.PostAsJsonAsync("/admin/auth/refresh", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<AdminRefreshTokenResponse>();
        Assert.NotNull(content);
        Assert.Equal("new-access-token-123", content.AccessToken);
        Assert.Equal("new-refresh-token-456", content.RefreshToken);
        Assert.Equal(3600, content.ExpiresIn);
    }

    [Fact]
    public async Task Refresh_WhenServiceThrowsUnauthorized_Returns401WithProblemDetails()
    {
        // Arrange
        var (client, fakeService) = CreateClientWithFakeService();

        fakeService.RefreshHandler = _ =>
            throw new UnauthorizedException(ErrorCodes.AuthRequired, "Phiên đăng nhập đã hết hạn hoặc refresh token không hợp lệ.");

        var request = new AdminRefreshTokenRequest("expired-token");

        // Act
        var response = await client.PostAsJsonAsync("/admin/auth/refresh", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(401, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.AuthRequired, content.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Refresh_WhenServiceThrowsForbidden_Returns403WithProblemDetails()
    {
        // Arrange
        var (client, fakeService) = CreateClientWithFakeService();

        fakeService.RefreshHandler = _ =>
            throw new ForbiddenException(ErrorCodes.Forbidden, "Tài khoản này không có quyền Quản trị viên (role: admin).");

        var request = new AdminRefreshTokenRequest("non-admin-token");

        // Act
        var response = await client.PostAsJsonAsync("/admin/auth/refresh", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(403, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.Forbidden, content.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_WhenValidCredentials_Returns200WithTokensAndRefreshToken()
    {
        // Arrange
        var (client, fakeService) = CreateClientWithFakeService();
        var expectedResponse = new AdminLoginResponse("access-token-abc", "admin@multiguide.vn", "admin", "refresh-token-xyz", 3600);

        fakeService.LoginHandler = (email, _) => Task.FromResult(expectedResponse);

        var request = new AdminLoginRequest("admin@multiguide.vn", "P@ssword123");

        // Act
        var response = await client.PostAsJsonAsync("/admin/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<AdminLoginResponse>();
        Assert.NotNull(content);
        Assert.Equal("access-token-abc", content.AccessToken);
        Assert.Equal("refresh-token-xyz", content.RefreshToken);
        Assert.Equal("admin", content.Role);
    }
}

