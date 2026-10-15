using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Data.Services;

/// <summary>
/// Hiện thực IAiService kết nối tới Google Gemini AI qua REST API của Google AI Studio (US-19).
/// </summary>
public class GeminiAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAiService> _logger;

    public GeminiAiService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GeneratePoiSummaryAsync(
        string poiName,
        string areaName,
        string language = "vi",
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"]
                     ?? _configuration["Gemini__ApiKey"];

        if (apiKey == null)
        {
            apiKey = Environment.GetEnvironmentVariable("Gemini__ApiKey")
                     ?? Environment.GetEnvironmentVariable("Gemini:ApiKey");
        }

        var model = _configuration["Gemini:Model"]
                    ?? _configuration["Gemini__Model"]
                    ?? Environment.GetEnvironmentVariable("Gemini__Model")
                    ?? "gemini-2.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("<"))
        {
            _logger.LogWarning("Gemini API key is not configured. Returning fallback mock summary.");
            return $"[Bản thảo AI] {poiName} là một trong những điểm tham quan tiêu biểu thuộc {areaName}. Nơi đây mang đậm dấu ấn lịch sử và giá trị văn hóa độc đáo, thu hút du khách đến tham quan và tìm hiểu các di sản truyền thống quý báu.";
        }

        var languageName = language.ToLowerInvariant() switch
        {
            "en" => "tiếng Anh (English)",
            "ja" => "tiếng Nhật (Japanese)",
            _ => "tiếng Việt (Vietnamese)"
        };

        var promptText = $"Bạn là hướng dẫn viên du lịch chuyên nghiệp. Hãy viết một đoạn tóm tắt lịch sử và ý nghĩa văn hóa ngắn gọn (khoảng 150-200 từ) về điểm tham quan '{poiName}' thuộc khu vực '{areaName}'. Đoạn văn cần truyền cảm hứng, chính xác và được viết bằng {languageName}. Chỉ trả về nội dung thuyết minh, không thêm lời chào hay định dạng markdown thừa.";

        var requestPayload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = promptText }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 800
            }
        };

        var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(apiKey)}";
        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var json = JsonSerializer.Serialize(requestPayload, jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API request failed with status code {StatusCode}: {Error}", response.StatusCode, errorContent);
                throw new AppException(ErrorCodes.InternalError, $"Lỗi phản hồi từ Google Gemini AI ({response.StatusCode}).");
            }

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var candidateContent) &&
                    candidateContent.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text.Trim();
                    }
                }
            }

            throw new AppException(ErrorCodes.InternalError, "Phản hồi từ Gemini AI không chứa nội dung hợp lệ.");
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Gemini AI for POI '{PoiName}'", poiName);
            throw new AppException(ErrorCodes.InternalError, "Không thể kết nối đến dịch vụ Google Gemini AI.", 500);
        }
    }
}
