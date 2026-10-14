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
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Domain.Constants;

namespace MultiGuide.Integration.Tests;

public class AdminPoisControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestIssuer = "https://test-supabase.co/auth/v1";
    private const string TestAudience = "authenticated";
    private const string TestSecret = "my_super_secret_test_jwt_key_32_bytes_long_123";

    private readonly HttpClient _client;

    public AdminPoisControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"AdminPois_{Guid.NewGuid()}");

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
        var handler = new JwtSecurityTokenHandler();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("email", "admin@multiguide.vn"),
            new("app_metadata", JsonSerializer.Serialize(new { role }), JsonClaimValueTypes.Json)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = TestIssuer,
            Audience = TestAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)),
                SecurityAlgorithms.HmacSha256Signature)
        };

        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null, string? token = null)
    {
        var message = new HttpRequestMessage(method, url);
        if (body != null)
        {
            message.Content = JsonContent.Create(body);
        }

        if (token != null)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return _client.SendAsync(message);
    }

    private async Task<AreaResponse> CreateAreaAsync(string token, string code)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/v1/admin/areas", new CreateAreaRequest
        {
            Code = code,
            Name = $"Khu {code}",
            CenterLat = 21.0,
            CenterLng = 105.8,
            RadiusM = 300,
            PriceUsd = 5m,
            PassTtlHours = 24
        }, token);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AreaResponse>())!;
    }

    [Fact]
    public async Task GetAll_ChuaDangNhap_TraVe401()
    {
        var response = await _client.GetAsync("/api/v1/admin/pois");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_KhongPhaiAdmin_TraVe403()
    {
        var response = await SendAsync(HttpMethod.Get, "/api/v1/admin/pois", token: GenerateJwt("user"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminPois_CRUD_Flow_ThanhCong()
    {
        var token = GenerateJwt("admin");
        var area = await CreateAreaAsync(token, "khu-poi-crud");

        // 1. POST
        var createResponse = await SendAsync(HttpMethod.Post, "/api/v1/admin/pois", new CreatePoiRequest
        {
            AreaId = area.Id,
            Lat = 21.0285,
            Lng = 105.8355,
            TriggerRadiusM = 40,
            Priority = 50,
            ImageUrl = null
        }, token);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<PoiResponse>();
        Assert.NotNull(created);
        Assert.Equal(area.Id, created.AreaId);
        Assert.Equal(40, created.TriggerRadiusM);
        Assert.Null(created.ImageUrl);
        Assert.True(created.IsActive);

        // 2. GET by id
        var getResponse = await SendAsync(HttpMethod.Get, $"/api/v1/admin/pois/{created.Id}", token: token);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // 3. PUT
        var putResponse = await SendAsync(HttpMethod.Put, $"/api/v1/admin/pois/{created.Id}", new UpdatePoiRequest
        {
            Lat = 21.03,
            Lng = 105.84,
            TriggerRadiusM = 60,
            Priority = 80,
            ImageUrl = "https://example.com/poi.jpg",
            IsActive = true
        }, token);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<PoiResponse>();
        Assert.NotNull(updated);
        Assert.Equal(60, updated.TriggerRadiusM);
        Assert.Equal("https://example.com/poi.jpg", updated.ImageUrl);

        // 4. DELETE (xóa mềm)
        var deleteResponse = await SendAsync(HttpMethod.Delete, $"/api/v1/admin/pois/{created.Id}", token: token);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await SendAsync(HttpMethod.Get, $"/api/v1/admin/pois/{created.Id}", token: token);
        Assert.Equal(HttpStatusCode.OK, afterDelete.StatusCode);
        var soft = await afterDelete.Content.ReadFromJsonAsync<PoiResponse>();
        Assert.NotNull(soft);
        Assert.False(soft.IsActive);
    }

    [Fact]
    public async Task Create_AreaKhongTonTai_TraVe404AreaNotFound()
    {
        var token = GenerateJwt("admin");

        var response = await SendAsync(HttpMethod.Post, "/api/v1/admin/pois", new CreatePoiRequest
        {
            AreaId = Guid.NewGuid(),
            Lat = 21,
            Lng = 105,
            TriggerRadiusM = 30
        }, token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ErrorCodes.AreaNotFound, json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Create_RadiusNgoaiKhoang_TraVe422ValidationError()
    {
        var token = GenerateJwt("admin");
        var area = await CreateAreaAsync(token, "khu-poi-invalid");

        var response = await SendAsync(HttpMethod.Post, "/api/v1/admin/pois", new CreatePoiRequest
        {
            AreaId = area.Id,
            Lat = 21,
            Lng = 105,
            TriggerRadiusM = 1000
        }, token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ErrorCodes.ValidationError, json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetAll_PhanTrangVaLocTheoKhu_HoatDongDung()
    {
        var token = GenerateJwt("admin");
        var areaA = await CreateAreaAsync(token, "khu-page-a");
        var areaB = await CreateAreaAsync(token, "khu-page-b");

        for (var i = 0; i < 3; i++)
        {
            var r = await SendAsync(HttpMethod.Post, "/api/v1/admin/pois", new CreatePoiRequest
            {
                AreaId = areaA.Id,
                Lat = 21,
                Lng = 105,
                TriggerRadiusM = 30,
                Priority = i
            }, token);
            Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        }

        var other = await SendAsync(HttpMethod.Post, "/api/v1/admin/pois", new CreatePoiRequest
        {
            AreaId = areaB.Id,
            Lat = 21,
            Lng = 105,
            TriggerRadiusM = 30
        }, token);
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);

        // Trang 1, size 2, lọc khu A
        var page1 = await SendAsync(HttpMethod.Get, $"/api/v1/admin/pois?areaId={areaA.Id}&page=1&page_size=2", token: token);
        Assert.Equal(HttpStatusCode.OK, page1.StatusCode);
        var p1 = await page1.Content.ReadFromJsonAsync<PagedResponse<PoiResponse>>();
        Assert.NotNull(p1);
        Assert.Equal(2, p1.Items.Count);
        Assert.Equal(3, p1.TotalCount);
        Assert.Equal(2, p1.TotalPages);
        Assert.All(p1.Items, x => Assert.Equal(areaA.Id, x.AreaId));
        Assert.True(p1.Items[0].Priority >= p1.Items[1].Priority);

        // Trang 2 còn 1 phần tử
        var page2 = await SendAsync(HttpMethod.Get, $"/api/v1/admin/pois?areaId={areaA.Id}&page=2&page_size=2", token: token);
        var p2 = await page2.Content.ReadFromJsonAsync<PagedResponse<PoiResponse>>();
        Assert.NotNull(p2);
        Assert.Single(p2.Items);
    }

    [Fact]
    public async Task GetAll_PageSizeLonHon100_TraVe422()
    {
        var token = GenerateJwt("admin");

        var response = await SendAsync(HttpMethod.Get, "/api/v1/admin/pois?page_size=101", token: token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ErrorCodes.ValidationError, json.GetProperty("code").GetString());
    }
}
