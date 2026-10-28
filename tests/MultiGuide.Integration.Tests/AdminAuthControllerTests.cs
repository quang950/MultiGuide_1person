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

        public Task<AdminLoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            if (LoginHandler != null) return LoginHandler(email, password);
            return Task.FromResult(new AdminLoginResponse("default-token", email, "admin", null, 2592000));
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
    public async Task Login_WhenValidCredentials_Returns200WithLongLivedToken()
    {
        // Arrange
        var (client, fakeService) = CreateClientWithFakeService();
        var expectedResponse = new AdminLoginResponse("access-token-abc", "admin@multiguide.vn", "admin", null, 2592000);

        fakeService.LoginHandler = (email, _) => Task.FromResult(expectedResponse);

        var request = new AdminLoginRequest("admin@multiguide.vn", "P@ssword123");

        // Act
        var response = await client.PostAsJsonAsync("/admin/auth/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<AdminLoginResponse>();
        Assert.NotNull(content);
        Assert.Equal("access-token-abc", content.AccessToken);
        Assert.Null(content.RefreshToken);
        Assert.Equal("admin", content.Role);
        Assert.Equal(2592000, content.ExpiresIn);
    }
}

