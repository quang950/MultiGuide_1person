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

public class AdminAreasControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestIssuer = "https://test-supabase.co/auth/v1";
    private const string TestAudience = "authenticated";
    private const string TestSecret = "my_super_secret_test_jwt_key_32_bytes_long_123";

    private readonly HttpClient _client;

    public AdminAreasControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"AdminAreas_{Guid.NewGuid()}");

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
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestSecret);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new("email", "admin@multiguide.vn"),
            new("app_metadata", JsonSerializer.Serialize(new { role }), JsonClaimValueTypes.Json)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = TestIssuer,
            Audience = TestAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task GetAll_ChuaDangNhap_TraVe401()
    {
        var response = await _client.GetAsync("/api/v1/admin/areas");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_KhongPhaiAdmin_TraVe403()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/areas");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GenerateJwt("user"));

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminAreas_CRUD_Flow_ThanhCong()
    {
        var adminToken = GenerateJwt("admin");

        // 1. POST: Tạo khu mới
        var createRequest = new CreateAreaRequest
        {
            Code = "hoang-thanh",
            Name = "Hoàng Thành Thăng Long",
            CenterLat = 21.0333,
            CenterLng = 105.8400,
            RadiusM = 500,
            PriceUsd = 10.0m,
            PassTtlHours = 24
        };

        var postMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/areas")
        {
            Content = JsonContent.Create(createRequest)
        };
        postMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var postResponse = await _client.SendAsync(postMsg);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var created = await postResponse.Content.ReadFromJsonAsync<AreaResponse>();
        Assert.NotNull(created);
        Assert.Equal("hoang-thanh", created.Code);
        Assert.Equal("Hoàng Thành Thăng Long", created.Name);
        Assert.True(created.IsActive);

        var areaId = created.Id;

        // 2. POST trùng code -> 409 DUPLICATE_REQUEST
        var duplicateMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/areas")
        {
            Content = JsonContent.Create(createRequest)
        };
        duplicateMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var duplicateResponse = await _client.SendAsync(duplicateMsg);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        var duplicateJson = await duplicateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ErrorCodes.DuplicateRequest, duplicateJson.GetProperty("code").GetString());

        // 3. GET by Id -> 200 OK
        var getByIdMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/areas/{areaId}");
        getByIdMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var getByIdResponse = await _client.SendAsync(getByIdMsg);
        Assert.Equal(HttpStatusCode.OK, getByIdResponse.StatusCode);

        var fetched = await getByIdResponse.Content.ReadFromJsonAsync<AreaResponse>();
        Assert.NotNull(fetched);
        Assert.Equal(areaId, fetched.Id);

        // 4. PUT: Cập nhật thông tin
        var updateRequest = new UpdateAreaRequest
        {
            Name = "Hoàng Thành Thăng Long (Cập nhật)",
            CenterLat = 21.0335,
            CenterLng = 105.8405,
            RadiusM = 550,
            PriceUsd = 12.5m,
            PassTtlHours = 48,
            IsActive = true
        };

        var putMsg = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/areas/{areaId}")
        {
            Content = JsonContent.Create(updateRequest)
        };
        putMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var putResponse = await _client.SendAsync(putMsg);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var updated = await putResponse.Content.ReadFromJsonAsync<AreaResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Hoàng Thành Thăng Long (Cập nhật)", updated.Name);
        Assert.Equal(12.5m, updated.PriceUsd);

        // 5. GET All -> Có chứa khu vừa tạo
        var getAllMsg = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/areas");
        getAllMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var getAllResponse = await _client.SendAsync(getAllMsg);
        Assert.Equal(HttpStatusCode.OK, getAllResponse.StatusCode);

        var list = await getAllResponse.Content.ReadFromJsonAsync<List<AreaResponse>>();
        Assert.NotNull(list);
        Assert.Contains(list, a => a.Id == areaId);

        // 6. DELETE: Xóa mềm
        var deleteMsg = new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/admin/areas/{areaId}");
        deleteMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var deleteResponse = await _client.SendAsync(deleteMsg);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 7. GET lại kiểm tra IsActive = false
        var verifyMsg = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/admin/areas/{areaId}");
        verifyMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var verifyResponse = await _client.SendAsync(verifyMsg);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var softDeleted = await verifyResponse.Content.ReadFromJsonAsync<AreaResponse>();
        Assert.NotNull(softDeleted);
        Assert.False(softDeleted.IsActive);
    }

    [Fact]
    public async Task Create_GiaNhoHonHoacBang0_TraVe422ValidationError()
    {
        var adminToken = GenerateJwt("admin");

        var createRequest = new CreateAreaRequest
        {
            Code = "khu-test-gia",
            Name = "Khu Test Giá",
            CenterLat = 21.0,
            CenterLng = 105.0,
            RadiusM = 100,
            PriceUsd = -5.0m,
            PassTtlHours = 24
        };

        var postMsg = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/areas")
        {
            Content = JsonContent.Create(createRequest)
        };
        postMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.SendAsync(postMsg);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ErrorCodes.ValidationError, json.GetProperty("code").GetString());
    }
}
