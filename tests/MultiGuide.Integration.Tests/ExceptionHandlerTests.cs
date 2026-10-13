using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Domain.Constants;
using MultiGuide.Integration.Tests.TestControllers;

namespace MultiGuide.Integration.Tests;

public class ExceptionHandlerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExceptionHandlerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"ErrorTest_{Guid.NewGuid()}");
            builder.ConfigureServices(services =>
            {
                services.AddControllers()
                    .AddApplicationPart(typeof(ErrorTestController).Assembly);
            });
        }).CreateClient();
    }

    [Fact]
    public async Task WhenNotFoundExceptionThrown_Returns404WithProblemDetailsAndCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test-errors/not-found");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(404, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.AreaNotFound, content.GetProperty("code").GetString());
        Assert.Equal("Tour area not found", content.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task WhenValidationExceptionThrown_Returns422WithCodeAndErrors()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test-errors/validation");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(422, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.ValidationError, content.GetProperty("code").GetString());
        Assert.True(content.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("lat", out _));
    }

    [Fact]
    public async Task WhenConflictExceptionThrown_Returns409WithCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test-errors/conflict");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(409, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.DuplicateRequest, content.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WhenUnauthorizedExceptionThrown_Returns401WithCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test-errors/unauthorized");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(401, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.PassMissing, content.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WhenForbiddenExceptionThrown_Returns403WithCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test-errors/forbidden");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(403, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.PassExpired, content.GetProperty("code").GetString());
    }

    [Fact]
    public async Task WhenUnhandledExceptionThrown_Returns500WithInternalErrorCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/test-errors/unhandled");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(500, content.GetProperty("status").GetInt32());
        Assert.Equal(ErrorCodes.InternalError, content.GetProperty("code").GetString());
    }

    [Fact]
    public async Task SwaggerEndpoint_Returns200OkWithSwaggerJson()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("MultiGuide API", json.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("v1", json.GetProperty("info").GetProperty("version").GetString());
    }
}
