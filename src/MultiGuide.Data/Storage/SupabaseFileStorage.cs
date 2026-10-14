using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiGuide.Business.Storage;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Data.Storage;

/// <summary>
/// Triển khai IFileStorage giao tiếp trực tiếp với Supabase Storage REST API qua HttpClient.
/// Hỗ trợ upload, xóa (idempotent), và tạo Signed URL cho private bucket.
/// </summary>
public class SupabaseFileStorage : IFileStorage
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseStorageOptions _options;
    private readonly ILogger<SupabaseFileStorage> _logger;


    public SupabaseFileStorage(
        HttpClient httpClient,
        IOptions<SupabaseStorageOptions> options,
        ILogger<SupabaseFileStorage> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> UploadAsync(
        Stream content,
        string path,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ValidateConfiguration();
        var cleanPath = NormalizePath(path);

        var bucket = string.IsNullOrWhiteSpace(_options.AudioBucket) ? "audio" : _options.AudioBucket;
        var requestUri = $"{GetStorageBaseUrl()}/object/{bucket}/{cleanPath}";

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        ApplyAuthHeaders(request);
        request.Headers.Add("x-upsert", "true");

        using var streamContent = new StreamContent(content);
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }
        request.Content = streamContent;

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Supabase Storage Upload thất bại HTTP {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            throw new AppException(ErrorCodes.StorageError, $"Không thể tải file lên lưu trữ Supabase (HTTP {(int)response.StatusCode}): {errorBody}", 500);
        }

        return cleanPath;
    }

    public async Task DeleteAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        var cleanPath = NormalizePath(path);

        var bucket = string.IsNullOrWhiteSpace(_options.AudioBucket) ? "audio" : _options.AudioBucket;
        var requestUri = $"{GetStorageBaseUrl()}/object/{bucket}/{cleanPath}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        ApplyAuthHeaders(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        // Tính idempotent: 200, 204 hoặc 404 (file không tồn tại) đều coi là xóa thành công
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Supabase Storage Delete thất bại HTTP {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            throw new AppException(ErrorCodes.StorageError, $"Không thể xóa file khỏi lưu trữ Supabase (HTTP {(int)response.StatusCode}): {errorBody}", 500);
        }
    }

    public async Task<string> CreateSignedUrlAsync(
        string path,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();
        var cleanPath = NormalizePath(path);

        var bucket = string.IsNullOrWhiteSpace(_options.AudioBucket) ? "audio" : _options.AudioBucket;
        var requestUri = $"{GetStorageBaseUrl()}/object/sign/{bucket}/{cleanPath}";

        var payload = new { expiresIn = (int)expiresIn.TotalSeconds };
        var jsonPayload = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        ApplyAuthHeaders(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Supabase Storage CreateSignedUrl thất bại HTTP {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            throw new AppException(ErrorCodes.StorageError, $"Không thể tạo signed URL từ Supabase (HTTP {(int)response.StatusCode}): {errorBody}", 500);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        string? signedPath = null;

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("signedURL", out var elem) ||
                    root.TryGetProperty("signedUrl", out elem) ||
                    root.TryGetProperty("SignedURL", out elem) ||
                    root.TryGetProperty("SignedUrl", out elem) ||
                    root.TryGetProperty("url", out elem))
                {
                    signedPath = elem.GetString();
                }
                else
                {
                    foreach (var prop in root.EnumerateObject())
                    {
                        if (prop.Name.Equals("signedURL", StringComparison.OrdinalIgnoreCase) ||
                            prop.Name.Equals("signedUrl", StringComparison.OrdinalIgnoreCase))
                        {
                            signedPath = prop.Value.GetString();
                            break;
                        }
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Không thể parse JSON phản hồi từ Supabase Storage: {ResponseBody}", responseBody);
            throw new AppException(ErrorCodes.StorageError, "Phản hồi signed URL từ Supabase không hợp lệ.", 500);
        }

        if (string.IsNullOrWhiteSpace(signedPath))
        {
            throw new AppException(ErrorCodes.StorageError, "Phản hồi signed URL từ Supabase không hợp lệ.", 500);
        }

        if (Uri.TryCreate(signedPath, UriKind.Absolute, out var absoluteUri) &&
            (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            return absoluteUri.ToString();
        }

        var baseUrl = _options.Url.TrimEnd('/');
        if (signedPath.StartsWith("/storage/v1", StringComparison.OrdinalIgnoreCase))
        {
            return $"{baseUrl}{signedPath}";
        }

        return $"{baseUrl}/storage/v1{(signedPath.StartsWith('/') ? signedPath : "/" + signedPath)}";
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.Url) || string.IsNullOrWhiteSpace(_options.ServiceRoleKey))
        {
            throw new AppException(
                ErrorCodes.StorageError,
                "Cấu hình Supabase Storage chưa hoàn chỉnh (thiếu Supabase:Url hoặc Supabase:ServiceRoleKey).",
                500);
        }
    }

    private string GetStorageBaseUrl()
    {
        return $"{_options.Url.TrimEnd('/')}/storage/v1";
    }

    private void ApplyAuthHeaders(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
        request.Headers.Remove("apikey");
        request.Headers.Add("apikey", _options.ServiceRoleKey);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Đường dẫn file không được để trống.");
        }

        var clean = path.Trim().TrimStart('/');
        if (string.IsNullOrWhiteSpace(clean))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Đường dẫn file không được để trống.");
        }

        return clean;
    }
}
