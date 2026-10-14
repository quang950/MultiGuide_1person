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
using MultiGuide.Data.Context;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Integration.Tests;

public class AdminTranslationsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestIssuer = "https://test-supabase.co/auth/v1";
    private const string TestAudience = "authenticated";
    private const string TestSecret = "my_super_secret_test_jwt_key_32_bytes_long_123";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AdminTranslationsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("USE_IN_MEMORY_DB", "true");
            builder.UseSetting("IN_MEMORY_DB_NAME", $"AdminTranslations_{Guid.NewGuid()}");

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
        });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        SeedLanguages();
    }

    private void SeedLanguages()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MultiGuideDbContext>();
        db.Languages.AddRange(
            new Language { Code = "vi", Name = "Tiếng Việt", IsActive = true },
            new Language { Code = "en", Name = "English", IsActive = true },
            new Language { Code = "ja", Name = "日本語", IsActive = false });
        db.SaveChanges();
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

    private async Task<PoiResponse> CreatePoiAsync(string token, string areaCode)
    {
        var areaResponse = await SendAsync(HttpMethod.Post, "/api/v1/admin/areas", new CreateAreaRequest
        {
            Code = areaCode,
            Name = $"Khu {areaCode}",
            CenterLat = 21.0,
            CenterLng = 105.8,
            RadiusM = 300,
            PriceUsd = 5m
        }, token);
        Assert.Equal(HttpStatusCode.Created, areaResponse.StatusCode);
        var area = (await areaResponse.Content.ReadFromJsonAsync<AreaResponse>())!;

        var poiResponse = await SendAsync(HttpMethod.Post, "/api/v1/admin/pois", new CreatePoiRequest
        {
            AreaId = area.Id,
            Lat = 21.0285,
            Lng = 105.8355,
            TriggerRadiusM = 30
        }, token);
        Assert.Equal(HttpStatusCode.Created, poiResponse.StatusCode);
        return (await poiResponse.Content.ReadFromJsonAsync<PoiResponse>())!;
    }

    [Fact]
    public async Task GetAll_ChuaDangNhap_TraVe401()
    {
        var response = await _client.GetAsync($"/api/v1/admin/pois/{Guid.NewGuid()}/translations");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_KhongPhaiAdmin_TraVe403()
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/v1/admin/pois/{Guid.NewGuid()}/translations", token: GenerateJwt("user"));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Translations_CRUD_Flow_ThanhCong()
    {
        var token = GenerateJwt("admin");
        var poi = await CreatePoiAsync(token, "khu-trans-crud");
        var baseUrl = $"/api/v1/admin/pois/{poi.Id}/translations";

        // 1. POST
        var createResponse = await SendAsync(HttpMethod.Post, baseUrl, new CreateTranslationRequest
        {
            LanguageCode = "vi",
            Title = "Khuê Văn Các",
            Description = "Biểu tượng của Hà Nội."
        }, token);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<TranslationResponse>();
        Assert.NotNull(created);
        Assert.Equal(poi.Id, created.PoiId);
        Assert.Equal("vi", created.LanguageCode);
        Assert.Null(created.AudioPath);

        // 2. GET by id
        var getResponse = await SendAsync(HttpMethod.Get, $"{baseUrl}/{created.Id}", token: token);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        // 3. PUT
        var putResponse = await SendAsync(HttpMethod.Put, $"{baseUrl}/{created.Id}", new UpdateTranslationRequest
        {
            Title = "Khuê Văn Các (cập nhật)",
            Description = "Nội dung mới."
        }, token);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);
        var updated = await putResponse.Content.ReadFromJsonAsync<TranslationResponse>();
        Assert.NotNull(updated);
        Assert.Equal("Khuê Văn Các (cập nhật)", updated.Title);

        // 4. GET list
        var listResponse = await SendAsync(HttpMethod.Get, baseUrl, token: token);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<TranslationResponse>>();
        Assert.NotNull(list);
        Assert.Single(list);

        // 5. DELETE
        var deleteResponse = await SendAsync(HttpMethod.Delete, $"{baseUrl}/{created.Id}", token: token);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await SendAsync(HttpMethod.Get, $"{baseUrl}/{created.Id}", token: token);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Create_TrungPoiVaNgonNgu_TraVe409TranslationExists()
    {
        var token = GenerateJwt("admin");
        var poi = await CreatePoiAsync(token, "khu-trans-dup");
        var url = $"/api/v1/admin/pois/{poi.Id}/translations";
        var body = new CreateTranslationRequest { LanguageCode = "en", Title = "Gate", Description = "Main gate." };

        var first = await SendAsync(HttpMethod.Post, url, body, token);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await SendAsync(HttpMethod.Post, url, body, token);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var json = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ErrorCodes.TranslationExists, json.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Create_CungNgonNguNhungPoiKhac_ThanhCong()
    {
        var token = GenerateJwt("admin");
        var poiA = await CreatePoiAsync(token, "khu-trans-a");
        var poiB = await CreatePoiAsync(token, "khu-trans-b");
        var body = new CreateTranslationRequest { LanguageCode = "vi", Title = "T", Description = "D" };

        var a = await SendAsync(HttpMethod.Post, $"/api/v1/admin/pois/{poiA.Id}/translations", body, token);
        var b = await SendAsync(HttpMethod.Post, $"/api/v1/admin/pois/{poiB.Id}/translations", body, token);

        Assert.Equal(HttpStatusCode.Created, a.StatusCode);
        Assert.Equal(HttpStatusCode.Created, b.StatusCode);
    }

    [Fact]
    public async Task Create_TitleQua200KyTu_TraVe422()
    {
        var token = GenerateJwt("admin");
        var poi = await CreatePoiAsync(token, "khu-trans-long");

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/admin/pois/{poi.Id}/translations", new CreateTranslationRequest
        {
            LanguageCode = "vi",
            Title = new string('a', 201),
            Description = "D"
        }, token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ErrorCodes.ValidationError, json.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("ja")]
    public async Task Create_NgonNguKhongTonTaiHoacBiTat_TraVe422(string languageCode)
    {
        var token = GenerateJwt("admin");
        var poi = await CreatePoiAsync(token, $"khu-trans-lang-{languageCode}");

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/admin/pois/{poi.Id}/translations", new CreateTranslationRequest
        {
            LanguageCode = languageCode,
            Title = "T",
            Description = "D"
        }, token);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Create_PoiKhongTonTai_TraVe404()
    {
        var token = GenerateJwt("admin");

        var response = await SendAsync(HttpMethod.Post, $"/api/v1/admin/pois/{Guid.NewGuid()}/translations", new CreateTranslationRequest
        {
            LanguageCode = "vi",
            Title = "T",
            Description = "D"
        }, token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_BanDichCuaPoiKhac_TraVe404()
    {
        var token = GenerateJwt("admin");
        var poiA = await CreatePoiAsync(token, "khu-trans-own-a");
        var poiB = await CreatePoiAsync(token, "khu-trans-own-b");

        var createResponse = await SendAsync(HttpMethod.Post, $"/api/v1/admin/pois/{poiA.Id}/translations",
            new CreateTranslationRequest { LanguageCode = "vi", Title = "T", Description = "D" }, token);
        var created = (await createResponse.Content.ReadFromJsonAsync<TranslationResponse>())!;

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/admin/pois/{poiB.Id}/translations/{created.Id}", token: token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
