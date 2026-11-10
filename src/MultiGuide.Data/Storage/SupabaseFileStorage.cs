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
            _logger.LogWarning("Supabase Storage Upload HTTP {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);

            // Nếu bị chặn bởi chính sách RLS Supabase (AccessDenied/policy), tự động lưu file cục bộ fallback an toàn
            if ((response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Forbidden) &&
                (errorBody.Contains("policy") || errorBody.Contains("AccessDenied") || errorBody.Contains("Unauthorized")))
            {
                var savedLocalPath = await SaveToLocalFallbackAsync(content, cleanPath, cancellationToken);
                return savedLocalPath;
            }

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

        if (cleanPath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) ||
            cleanPath.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            // Xóa file local nếu có
            try
            {
                var localFile = Path.Combine(Directory.GetCurrentDirectory(), "client", cleanPath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(localFile))
                {
                    File.Delete(localFile);
                }
            }
            catch { }
            return;
        }

        var bucket = string.IsNullOrWhiteSpace(_options.AudioBucket) ? "audio" : _options.AudioBucket;
        var requestUri = $"{GetStorageBaseUrl()}/object/{bucket}/{cleanPath}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, requestUri);
        ApplyAuthHeaders(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        // Tính idempotent: 200, 204 hoặc 404 (file không tồn tại) đều coi là xóa thành công
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (errorBody.Contains("not_found", StringComparison.OrdinalIgnoreCase) ||
                errorBody.Contains("NoSuchKey", StringComparison.OrdinalIgnoreCase) ||
                errorBody.Contains("AccessDenied", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Supabase Storage Delete bỏ qua do file không tồn tại hoặc RLS: {ErrorBody}", errorBody);
                return;
            }
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

        if (cleanPath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) ||
            cleanPath.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
        {
            return cleanPath;
        }

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
            _logger.LogWarning("Supabase Storage CreateSignedUrl HTTP {StatusCode}: {ErrorBody}. Kiểm tra fallback cục bộ.", response.StatusCode, errorBody);

            var localCandidate = TryResolveLocalPath(cleanPath);
            if (localCandidate != null)
            {
                return localCandidate;
            }

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

    private async Task<string> SaveToLocalFallbackAsync(Stream content, string cleanPath, CancellationToken cancellationToken)
    {
        try
        {
            var parts = cleanPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && cleanPath.StartsWith("pois/", StringComparison.OrdinalIgnoreCase))
            {
                var poiId = parts[1];
                var langWithExt = parts[^1];
                var lang = Path.GetFileNameWithoutExtension(langWithExt);
                var localFileName = $"{poiId}_{lang}.mp3";
                var localSimple = $"{poiId}.mp3";

                var targetDir = Path.Combine(Directory.GetCurrentDirectory(), "client", "assets", "audio", "pois");
                if (!Directory.Exists(targetDir))
                {
                    var parent = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.FullName;
                    if (parent != null)
                    {
                        targetDir = Path.Combine(parent, "client", "assets", "audio", "pois");
                    }
                }

                if (Directory.Exists(targetDir))
                {
                    if (content.CanSeek) content.Position = 0;
                    var filePath = Path.Combine(targetDir, localFileName);
                    await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                    {
                        await content.CopyToAsync(fs, cancellationToken);
                    }

                    if (string.Equals(lang, "vi", StringComparison.OrdinalIgnoreCase))
                    {
                        var simplePath = Path.Combine(targetDir, localSimple);
                        File.Copy(filePath, simplePath, overwrite: true);
                    }

                    _logger.LogInformation("Đã lưu audio fallback thành công tại: {FilePath}", filePath);
                    return $"assets/audio/pois/{localFileName}";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi khi lưu audio fallback vào thư mục cục bộ.");
        }
        return cleanPath;
    }

    private static string? TryResolveLocalPath(string cleanPath)
    {
        var parts = cleanPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var poiId = parts[1];
            var lang = Path.GetFileNameWithoutExtension(parts[^1]);
            var localCandidate = $"assets/audio/pois/{poiId}_{lang}.mp3";
            var simpleCandidate = $"assets/audio/pois/{poiId}.mp3";

            var root = Directory.GetCurrentDirectory();
            var p1 = Path.Combine(root, "client", localCandidate);
            var p2 = Path.Combine(root, "client", simpleCandidate);
            if (File.Exists(p1)) return localCandidate;
            if (File.Exists(p2)) return simpleCandidate;

            var parent = Directory.GetParent(root)?.Parent?.FullName;
            if (parent != null)
            {
                if (File.Exists(Path.Combine(parent, "client", localCandidate))) return localCandidate;
                if (File.Exists(Path.Combine(parent, "client", simpleCandidate))) return simpleCandidate;
            }
        }
        return null;
    }
}
