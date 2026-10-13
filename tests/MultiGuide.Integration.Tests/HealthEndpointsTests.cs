using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MultiGuide.Integration.Tests;

public class HealthEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_LivenessProbe_Returns200Ok()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"Health_Liveness_{Guid.NewGuid()}");
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", content.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetHealthReady_WhenDbIsAvailable_Returns200OkWithReadyStatus()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"Health_Ready_{Guid.NewGuid()}");
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ready", content.GetProperty("status").GetString());
        Assert.Equal("healthy", content.GetProperty("database").GetString());
    }

    [Fact]
    public async Task GetHealthReady_WhenDbFails_Returns503ServiceUnavailable()
    {
        // Arrange
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "false");
            builder.UseSetting("DATABASE_URL", "Host=127.0.0.1;Port=1;Database=nonexistent;Username=none;Password=none;Timeout=1;CommandTimeout=1");
        }).CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("unhealthy", content.GetProperty("status").GetString());
        Assert.Equal("unhealthy", content.GetProperty("database").GetString());
    }
}
