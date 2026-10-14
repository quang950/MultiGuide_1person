using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Data.Context;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Enums;

namespace MultiGuide.Integration.Tests;

public class PassEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PassEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private (HttpClient client, string dbName) CreateTestClient(string? paymentMockResult = null)
    {
        var dbName = $"PassEndpoints_{Guid.NewGuid()}";
        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", dbName);
            if (paymentMockResult != null)
            {
                builder.UseSetting("Payment:MockResult", paymentMockResult);
            }
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        return (client, dbName);
    }

    private async Task SeedAreaAsync(string dbName, string code, decimal priceUsd = 5.0m, int ttlHours = 24, bool isActive = true)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MultiGuideDbContext>();

        // Set DB name context if required or recreate scope context
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
            PriceUsd = priceUsd,
            PassTtlHours = ttlHours,
            IsActive = isActive
        });

        await isolatedContext.SaveChangesAsync();
    }

    [Fact]
    public async Task CreatePass_RequestHopLe_TraVe201CreatedVaTokenVaLuuDb()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        await SeedAreaAsync(dbName, "van-mieu", priceUsd: 5.0m, ttlHours: 24);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes");
        request.Headers.Add("Idempotency-Key", "key-success-201");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var pass = await response.Content.ReadFromJsonAsync<CreatePassResponse>();
        Assert.NotNull(pass);
        Assert.Equal("van-mieu", pass.AreaCode);
        Assert.Equal(5.0m, pass.PriceUsd);
        Assert.False(string.IsNullOrWhiteSpace(pass.AccessToken));
        Assert.Equal(64, pass.AccessToken.Length);
        Assert.True(pass.ExpiresAt > DateTime.UtcNow);

        // Kiểm tra CSDL
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var db = new MultiGuideDbContext(options);

        var savedPayment = await db.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == "key-success-201");
        Assert.NotNull(savedPayment);
        Assert.Equal(PaymentStatus.Succeeded, savedPayment.Status);
        Assert.Equal(5.0m, savedPayment.Amount);

        var savedPass = await db.AccessPasses.FirstOrDefaultAsync(p => p.PaymentId == savedPayment.Id);
        Assert.NotNull(savedPass);
        Assert.Equal(AccessPassStatus.Active, savedPass.Status);

        // TokenHash phải là SHA-256 của AccessToken
        var expectedHashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(pass.AccessToken));
        var expectedHash = Convert.ToHexString(expectedHashBytes).ToLowerInvariant();
        Assert.Equal(expectedHash, savedPass.TokenHash);
    }

    [Fact]
    public async Task CreatePass_ThieuHeaderIdempotencyKey_TraVe422ValidationError()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        await SeedAreaAsync(dbName, "van-mieu");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes");
        // Không truyền header Idempotency-Key

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("code", out var codeElement));
        Assert.Equal(ErrorCodes.ValidationError, codeElement.GetString());
    }

    [Fact]
    public async Task CreatePass_TrungLapIdempotencyKey_TraVe409DuplicateRequest()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        await SeedAreaAsync(dbName, "van-mieu");

        const string duplicateKey = "key-repeat-123";

        // Gửi lần 1
        using (var req1 = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes"))
        {
            req1.Headers.Add("Idempotency-Key", duplicateKey);
            var res1 = await client.SendAsync(req1);
            Assert.Equal(HttpStatusCode.Created, res1.StatusCode);
        }

        // Gửi lần 2 với cùng key
        using (var req2 = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes"))
        {
            req2.Headers.Add("Idempotency-Key", duplicateKey);
            var res2 = await client.SendAsync(req2);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);

            var json = await res2.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.TryGetProperty("code", out var codeElement));
            Assert.Equal(ErrorCodes.DuplicateRequest, codeElement.GetString());
        }
    }

    [Fact]
    public async Task CreatePass_CongThanhToanTuChoi_TraVe402PaymentFailed()
    {
        // Arrange - Cổng giả lập cấu hình từ chối thanh toán
        var (client, dbName) = CreateTestClient(paymentMockResult: "failed");
        await SeedAreaAsync(dbName, "van-mieu");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes");
        request.Headers.Add("Idempotency-Key", "key-should-fail");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("code", out var codeElement));
        Assert.Equal(ErrorCodes.PaymentFailed, codeElement.GetString());

        // Kiểm tra CSDL: Payment phải lưu Status = Failed và AccessPass không được tạo
        var options = new DbContextOptionsBuilder<MultiGuideDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var db = new MultiGuideDbContext(options);

        var savedPayment = await db.Payments.FirstOrDefaultAsync(p => p.IdempotencyKey == "key-should-fail");
        Assert.NotNull(savedPayment);
        Assert.Equal(PaymentStatus.Failed, savedPayment.Status);

        var passesCount = await db.AccessPasses.CountAsync();
        Assert.Equal(0, passesCount);
    }

    [Fact]
    public async Task CreatePass_KhuKhongTonTai_TraVe404AreaNotFound()
    {
        // Arrange
        var (client, _) = CreateTestClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/areas/khu-khong-co-that/passes");
        request.Headers.Add("Idempotency-Key", "key-area-404");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("code", out var codeElement));
        Assert.Equal(ErrorCodes.AreaNotFound, codeElement.GetString());
    }

    [Fact]
    public async Task CreatePass_KhuBiTat_TraVe404AreaNotFound()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        await SeedAreaAsync(dbName, "khu-tat", isActive: false);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/areas/khu-tat/passes");
        request.Headers.Add("Idempotency-Key", "key-area-inactive");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("code", out var codeElement));
        Assert.Equal(ErrorCodes.AreaNotFound, codeElement.GetString());
    }

    [Fact]
    public async Task CreatePass_HoTroPrefixApiV1_TraVe201()
    {
        // Arrange
        var (client, dbName) = CreateTestClient();
        await SeedAreaAsync(dbName, "van-mieu");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/areas/van-mieu/passes");
        request.Headers.Add("Idempotency-Key", "key-api-v1-prefix");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
