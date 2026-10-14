using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Integration.Tests;

public class RateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RateLimitingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (HttpClient client, string dbName) CreateTestClient()
    {
        var dbName = $"RateLimiting_{Guid.NewGuid()}";
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", dbName);
            builder.UseSetting("Payment:MockResult", "success");
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        return (client, dbName);
    }

    private async Task SeedAreaAsync(string dbName, string code)
    {
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var isolatedContext = new MultiGuideDbContext(options);

        isolatedContext.TourAreas.Add(new TourArea
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = $"Khu {code}",
            CenterLat = 21.0285,
            CenterLng = 105.8355,
            RadiusM = 300,
            PriceUsd = 5.0m,
            PassTtlHours = 24,
            IsActive = true
        });

        await isolatedContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CreatePass_GoiQua5LanTrong1PhutCungIP_TraVe429TooManyRequestsVaProblemDetails()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        await SeedAreaAsync(dbName, "van-mieu");

        var clientIp = $"198.51.100.{Random.Shared.Next(1, 200)}";

        // Act & Assert: 5 request đầu tiên thành công 201 Created
        for (var i = 1; i <= 5; i++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes");
            req.Headers.Add("Idempotency-Key", $"key-ratelimit-{clientIp}-{i}");
            req.Headers.Add("X-Forwarded-For", clientIp);

            var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);

            var pass = await res.Content.ReadFromJsonAsync<CreatePassResponse>();
            Assert.NotNull(pass);
        }

        // Request thứ 6 từ cùng IP -> Bị chặn với 429 Too Many Requests
        using var blockedReq = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes");
        blockedReq.Headers.Add("Idempotency-Key", $"key-ratelimit-{clientIp}-6");
        blockedReq.Headers.Add("X-Forwarded-For", clientIp);

        var blockedRes = await client.SendAsync(blockedReq);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, blockedRes.StatusCode);
        Assert.Equal("application/problem+json", blockedRes.Content.Headers.ContentType?.MediaType);

        var body = await blockedRes.Content.ReadAsStringAsync();
        var problem = JsonSerializer.Deserialize<ProblemDetails>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(problem);
        Assert.Equal(429, problem.Status);
        Assert.Equal("Too Many Requests", problem.Title);
        Assert.True(problem.Extensions.ContainsKey("code"));
        Assert.Equal(ErrorCodes.RateLimited, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task CreatePass_HaiIPKhacNhau_DuocGioiHanDocLap()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        await SeedAreaAsync(dbName, "van-mieu");

        var ipA = $"198.51.101.{Random.Shared.Next(1, 100)}";
        var ipB = $"198.51.102.{Random.Shared.Next(101, 200)}";

        // IP A dùng hết 5 lượt
        for (var i = 1; i <= 5; i++)
        {
            using var reqA = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes");
            reqA.Headers.Add("Idempotency-Key", $"key-ipA-{ipA}-{i}");
            reqA.Headers.Add("X-Forwarded-For", ipA);

            var resA = await client.SendAsync(reqA);
            Assert.Equal(HttpStatusCode.Created, resA.StatusCode);
        }

        // IP A bị chặn ở lần 6
        using var blockedReqA = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes");
        blockedReqA.Headers.Add("Idempotency-Key", $"key-ipA-{ipA}-6");
        blockedReqA.Headers.Add("X-Forwarded-For", ipA);
        var blockedResA = await client.SendAsync(blockedReqA);
        Assert.Equal(HttpStatusCode.TooManyRequests, blockedResA.StatusCode);

        // IP B gửi lần đầu -> Vẫn thành công 201 Created (không bị ảnh hưởng bởi IP A)
        using var reqB = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes");
        reqB.Headers.Add("Idempotency-Key", $"key-ipB-{ipB}-1");
        reqB.Headers.Add("X-Forwarded-For", ipB);
        var resB = await client.SendAsync(reqB);
        Assert.Equal(HttpStatusCode.Created, resB.StatusCode);
    }
}
