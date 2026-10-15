using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MultiGuide.Integration.Tests;

public class CorsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CorsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("http://localhost:3000")]
    [InlineData("http://127.0.0.1:5500")]
    [InlineData("https://multiguide-client.vercel.app")]
    [InlineData("https://test-preview.vercel.app")]
    public async Task Cors_AllowedOrigin_ReturnsAccessControlAllowOriginHeader(string origin)
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"Cors_Allowed_{Guid.NewGuid()}");
        }).CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", origin);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        var allowOrigin = response.Headers.GetValues("Access-Control-Allow-Origin").FirstOrDefault();
        Assert.Equal(origin, allowOrigin);
    }

    [Fact]
    public async Task Cors_PreflightRequest_ReturnsSuccessAndAllowHeaders()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"Cors_Preflight_{Guid.NewGuid()}");
        }).CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Options, "/areas/van-mieu");
        request.Headers.Add("Origin", "https://multiguide-demo.vercel.app");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK);
        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal("https://multiguide-demo.vercel.app", response.Headers.GetValues("Access-Control-Allow-Origin").First());
    }

    [Fact]
    public async Task Cors_DisallowedOrigin_DoesNotReturnAccessControlAllowOrigin()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"Cors_Disallowed_{Guid.NewGuid()}");
        }).CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://malicious-website.com");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
