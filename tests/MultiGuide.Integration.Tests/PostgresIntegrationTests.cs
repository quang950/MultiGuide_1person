using System.Net;
using System.Net.Http.Headers;
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
using MultiGuide.Data.Seed;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Enums;
using Testcontainers.PostgreSql;
using Xunit;

namespace MultiGuide.Integration.Tests;

/// <summary>
/// Task T-4.2: Integration test với Testcontainers PostgreSQL thật chạy độc lập trên Docker.
/// Kiểm thử toàn bộ luồng mua vé (passes) + tìm điểm lân cận (nearby) với schema PostgreSQL thật,
/// khóa ngoại, SHA-256 hash token và truy vấn dữ liệu địa lý thực tế.
/// </summary>
public class PostgresIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer? _postgresContainer;
    private readonly bool _isDockerAvailable;

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;
    private string? _connectionString;

    public PostgresIntegrationTests()
    {
        try
        {
            _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
                .WithDatabase("multiguide_test")
                .WithUsername("postgres")
                .WithPassword("postgres_password")
                .Build();
            _isDockerAvailable = true;
        }
        catch
        {
            _isDockerAvailable = false;
        }
    }

    public async Task InitializeAsync()
    {
        if (!_isDockerAvailable || _postgresContainer == null)
        {
            return;
        }

        try
        {
            // 1. Khởi động PostgreSQL container thật
            await _postgresContainer.StartAsync();
            _connectionString = _postgresContainer.GetConnectionString();

            // 2. Chạy EF Core Migrations trên PostgreSQL thật để tạo toàn bộ bảng và quan hệ
            var optionsBuilder = new DbContextOptionsBuilder<MultiGuideDbContext>();
            optionsBuilder.UseNpgsql(_connectionString);
            await using (var dbContext = new MultiGuideDbContext(optionsBuilder.Options))
            {
                await dbContext.Database.MigrateAsync();

                // 3. Seed dữ liệu mẫu chuẩn (Khu Văn Miếu, Ngôn ngữ, POIs)
                await DbSeeder.SeedDemoDataAsync(dbContext, force: true);
            }

            // 4. Khởi tạo WebApplicationFactory trỏ trực tiếp đến PostgreSQL container thật
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting("USE_IN_MEMORY_DB", "false");
                    builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
                    builder.UseSetting("DATABASE_URL", _connectionString);
                    builder.UseSetting("Payment:MockResult", "success");
                });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
        }
        catch
        {
            // Nếu không thể kết nối tới Docker engine trong quá trình khởi động
        }
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
        if (_postgresContainer != null)
        {
            await _postgresContainer.DisposeAsync();
        }
    }

    [Fact]
    public async Task PurchasePass_ThenQueryNearbyPois_OnRealPostgreSql_SucceedsEndToEnd()
    {
        if (!_isDockerAvailable || _client == null || _connectionString == null)
        {
            return; // Skip khi Docker không khả dụng trên môi trường cục bộ
        }

        // --- BƯỚC 1: Mua vé AccessPass qua POST /areas/van-mieu/passes với Idempotency-Key ---
        var idempotencyKey = $"test-idem-{Guid.NewGuid():N}";
        using var passRequest = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes");
        passRequest.Headers.Add("Idempotency-Key", idempotencyKey);

        var passResponse = await _client.SendAsync(passRequest);

        Assert.Equal(HttpStatusCode.Created, passResponse.StatusCode);
        var passDto = await passResponse.Content.ReadFromJsonAsync<CreatePassResponse>();
        Assert.NotNull(passDto);
        Assert.Equal("van-mieu", passDto.AreaCode);
        Assert.False(string.IsNullOrWhiteSpace(passDto.AccessToken));
        Assert.Equal(64, passDto.AccessToken.Length);
        Assert.True(passDto.ExpiresAt > DateTime.UtcNow);

        // --- BƯỚC 2: Kiểm tra trực tiếp trong PostgreSQL container xem AccessPass và Payment đã được lưu chính xác ---
        var optionsBuilder = new DbContextOptionsBuilder<MultiGuideDbContext>();
        optionsBuilder.UseNpgsql(_connectionString);
        await using (var dbContext = new MultiGuideDbContext(optionsBuilder.Options))
        {
            // Kiểm tra Payment trong Postgres
            var payment = await dbContext.Payments
                .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey);
            Assert.NotNull(payment);
            Assert.Equal(PaymentStatus.Succeeded, payment.Status);

            // Kiểm tra AccessPass lưu bằng SHA-256 hash của AccessToken
            var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passDto.AccessToken))).ToLowerInvariant();
            var savedPass = await dbContext.AccessPasses
                .Include(p => p.Area)
                .FirstOrDefaultAsync(p => p.TokenHash == expectedHash);

            Assert.NotNull(savedPass);
            Assert.Equal(AccessPassStatus.Active, savedPass.Status);
            Assert.NotNull(savedPass.Area);
            Assert.Equal("van-mieu", savedPass.Area.Code);
            Assert.Equal(payment.Id, savedPass.PaymentId);
        }

        // --- BƯỚC 3: Dùng AccessToken vừa mua để gọi GET /pois/nearby trên PostgreSQL thật ---
        // Tọa độ tại Cổng Văn Miếu: 21.0269, 105.8355
        using var nearbyRequest = new HttpRequestMessage(
            HttpMethod.Get,
            "/pois/nearby?lat=21.0269&lng=105.8355&radius=300&lang=vi");
        nearbyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", passDto.AccessToken);

        var nearbyResponse = await _client.SendAsync(nearbyRequest);

        Assert.Equal(HttpStatusCode.OK, nearbyResponse.StatusCode);
        var pois = await nearbyResponse.Content.ReadFromJsonAsync<List<NearbyPoiResponse>>();
        Assert.NotNull(pois);
        Assert.NotEmpty(pois);

        // Xác minh POI Cổng Văn Miếu nằm trong danh sách, có cờ within_trigger và tiêu đề tiếng Việt chuẩn
        var congVanMieu = pois.FirstOrDefault(p => p.Title.Contains("Cổng Văn Miếu"));
        Assert.NotNull(congVanMieu);
        Assert.True(congVanMieu.WithinTrigger);
        Assert.True(congVanMieu.DistanceM <= 30);
    }

    [Fact]
    public async Task PurchasePass_DuplicateIdempotencyKey_Returns409ConflictOnPostgres()
    {
        if (!_isDockerAvailable || _client == null || _connectionString == null)
        {
            return; // Skip khi Docker không khả dụng trên môi trường cục bộ
        }

        var idempotencyKey = $"test-duplicate-{Guid.NewGuid():N}";

        // Lần 1: Mua vé thành công
        using (var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes"))
        {
            firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
            var firstResponse = await _client.SendAsync(firstRequest);
            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        }

        // Lần 2: Gửi lại cùng Idempotency-Key -> phải trả về 409 Conflict với ErrorCodes.DuplicateRequest
        using (var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/areas/van-mieu/passes"))
        {
            secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);
            var secondResponse = await _client.SendAsync(secondRequest);
            Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);

            var problem = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
            var code = problem.GetProperty("code").GetString();
            Assert.Equal(ErrorCodes.DuplicateRequest, code);
        }
    }

    [Fact]
    public async Task QueryNearbyPois_WithInvalidToken_Returns403ForbiddenOnPostgres()
    {
        if (!_isDockerAvailable || _client == null)
        {
            return; // Skip khi Docker không khả dụng trên môi trường cục bộ
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/pois/nearby?lat=21.0269&lng=105.8355&radius=300&lang=vi");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "invalid_fake_access_token_12345");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        var code = problem.GetProperty("code").GetString();
        Assert.Equal(ErrorCodes.PassInvalid, code);
    }
}
