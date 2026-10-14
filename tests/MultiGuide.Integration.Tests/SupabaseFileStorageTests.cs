using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MultiGuide.Data.Storage;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Integration.Tests;

public class SupabaseFileStorageTests
{
    private const string TestBaseUrl = "https://test-supabase.co";
    private const string TestServiceKey = "test_service_role_key_secret_123";
    private const string TestBucket = "audio";

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Handler { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK);
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return Handler(request);
        }
    }

    private (SupabaseFileStorage storage, MockHttpMessageHandler handler) CreateStorage(
        string url = TestBaseUrl,
        string serviceKey = TestServiceKey,
        string bucket = TestBucket)
    {
        var mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHandler);
        var options = Options.Create(new SupabaseStorageOptions
        {
            Url = url,
            ServiceRoleKey = serviceKey,
            AudioBucket = bucket
        });
        var logger = NullLogger<SupabaseFileStorage>.Instance;

        var storage = new SupabaseFileStorage(httpClient, options, logger);
        return (storage, mockHandler);
    }

    [Fact]
    public async Task UploadAsync_KhiHopLe_GoiDungEndpointVaHeaderVaTraVePath()
    {
        // Arrange
        var (storage, handler) = CreateStorage();
        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Key\": \"audio/pois/1/vi.mp3\"}", Encoding.UTF8, "application/json")
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake mp3 data"));

        // Act
        var result = await storage.UploadAsync(stream, "/pois/1/vi.mp3", "audio/mpeg");

        // Assert
        Assert.Equal("pois/1/vi.mp3", result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("https://test-supabase.co/storage/v1/object/audio/pois/1/vi.mp3", handler.LastRequest.RequestUri?.ToString());
        Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization?.Scheme);
        Assert.Equal(TestServiceKey, handler.LastRequest.Headers.Authorization?.Parameter);
        Assert.Contains(TestServiceKey, handler.LastRequest.Headers.GetValues("apikey"));
        Assert.Contains("true", handler.LastRequest.Headers.GetValues("x-upsert"));
    }

    [Fact]
    public async Task UploadAsync_KhiHttpLoi500_NemAppExceptionVoiStorageError()
    {
        // Arrange
        var (storage, handler) = CreateStorage();
        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal server error from Supabase")
        };

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake data"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            storage.UploadAsync(stream, "pois/1/vi.mp3", "audio/mpeg"));

        Assert.Equal(ErrorCodes.StorageError, ex.Code);
        Assert.Equal(500, ex.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/")]
    public async Task UploadAsync_KhiDuongDanRong_NemValidationException(string invalidPath)
    {
        // Arrange
        var (storage, _) = CreateStorage();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake data"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            storage.UploadAsync(stream, invalidPath, "audio/mpeg"));

        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
    }

    [Fact]
    public async Task UploadAsync_KhiChuaCauHinhKey_NemAppException()
    {
        // Arrange
        var (storage, _) = CreateStorage(serviceKey: "");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake data"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            storage.UploadAsync(stream, "pois/1/vi.mp3", "audio/mpeg"));

        Assert.Equal(ErrorCodes.StorageError, ex.Code);
    }

    [Fact]
    public async Task DeleteAsync_KhiThanhCong_GoiDungEndpointDelete()
    {
        // Arrange
        var (storage, handler) = CreateStorage();
        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        await storage.DeleteAsync("pois/1/vi.mp3");

        // Assert
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest.Method);
        Assert.Equal("https://test-supabase.co/storage/v1/object/audio/pois/1/vi.mp3", handler.LastRequest.RequestUri?.ToString());
        Assert.Equal(TestServiceKey, handler.LastRequest.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task DeleteAsync_KhiFileKhongTonTai404_KhongNemLoi_Idempotent()
    {
        // Arrange
        var (storage, handler) = CreateStorage();
        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        // Act & Assert - Không ném ngoại lệ
        await storage.DeleteAsync("pois/1/nonexistent.mp3");
    }

    [Fact]
    public async Task DeleteAsync_KhiHttpLoi500_NemAppException()
    {
        // Arrange
        var (storage, handler) = CreateStorage();
        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Delete error")
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            storage.DeleteAsync("pois/1/vi.mp3"));

        Assert.Equal(ErrorCodes.StorageError, ex.Code);
        Assert.Equal(500, ex.StatusCode);
    }

    [Fact]
    public async Task CreateSignedUrlAsync_KhiThanhCong_TraVeFullUrlChinhXac()
    {
        // Arrange
        var (storage, handler) = CreateStorage();
        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"signedURL\": \"/object/sign/audio/pois/1/vi.mp3?token=eyJhbGciOi...\"}", Encoding.UTF8, "application/json")
        };

        // Act
        var result = await storage.CreateSignedUrlAsync("pois/1/vi.mp3", TimeSpan.FromHours(1));

        // Assert
        Assert.Equal("https://test-supabase.co/storage/v1/object/sign/audio/pois/1/vi.mp3?token=eyJhbGciOi...", result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Equal("https://test-supabase.co/storage/v1/object/sign/audio/pois/1/vi.mp3", handler.LastRequest.RequestUri?.ToString());
        Assert.Contains("3600", handler.LastRequestBody);
    }

    [Fact]
    public async Task CreateSignedUrlAsync_KhiTraVeCamelCaseSignedUrl_VanParseDung()
    {
        // Arrange
        var (storage, handler) = CreateStorage();
        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"signedUrl\": \"/object/sign/audio/pois/1/en.mp3?token=token123\"}", Encoding.UTF8, "application/json")
        };

        // Act
        var result = await storage.CreateSignedUrlAsync("pois/1/en.mp3", TimeSpan.FromMinutes(30));

        // Assert
        Assert.Equal("https://test-supabase.co/storage/v1/object/sign/audio/pois/1/en.mp3?token=token123", result);
        Assert.Contains("1800", handler.LastRequestBody);
    }

    [Fact]
    public async Task CreateSignedUrlAsync_KhiHttpLoi_NemAppException()
    {
        // Arrange
        var (storage, handler) = CreateStorage();
        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Invalid expire time")
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            storage.CreateSignedUrlAsync("pois/1/vi.mp3", TimeSpan.FromHours(1)));

        Assert.Equal(ErrorCodes.StorageError, ex.Code);
        Assert.Equal(500, ex.StatusCode);
    }
}
