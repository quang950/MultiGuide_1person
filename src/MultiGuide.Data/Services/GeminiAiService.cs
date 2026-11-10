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
            _logger.LogWarning("Gemini API key is not configured. Returning smart fallback narration.");
            return GenerateSmartFallbackNarration(poiName, areaName, language);
        }

        var languageName = language.ToLowerInvariant() switch
        {
            "en" => "tiếng Anh (English)",
            "ja" => "tiếng Nhật (Japanese)",
            _ => "tiếng Việt (Vietnamese)"
        };

        var promptText = $"Bạn là hướng dẫn viên du lịch và chuyên gia văn hóa di sản. Hãy viết một bài thuyết minh truyền cảm cho điểm tham quan '{poiName}' thuộc khu vực '{areaName}' bằng {languageName}.\n" +
                         "Yêu cầu cấu trúc bài gồm đúng 3 phần cách nhau bằng 2 dấu xuống dòng:\n" +
                         "- Phần 1: Đoạn thuyết minh ngắn gọn về lịch sử, kiến trúc hoặc cảnh quan nổi bật (khoảng 2-3 câu).\n" +
                         "- Phần 2: Một câu trích dẫn văn hóa, danh ngôn, ca dao hoặc câu nói văn học đặc sắc lột tả linh hồn của địa danh, đặt trong dấu ngoặc kép dạng \"...\".\n" +
                         "- Phần 3: Đoạn suy ngẫm hoặc cảm nhận sâu sắc đọng lại cho du khách (khoảng 1-2 câu).\n" +
                         "Chỉ trả về nội dung thuyết minh theo 3 phần trên, không thêm lời chào, không dùng ký hiệu markdown (#, **).";

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

    private static string GenerateSmartFallbackNarration(string poiName, string areaName, string language)
    {
        var lower = poiName.ToLowerInvariant();
        string text;

        if (lower.Contains("bà đen") || lower.Contains("ba den"))
        {
            text = "Núi Bà Đen cao 986 mét sừng sững giữa mây ngàn Tây Ninh, được mệnh danh là \"Đệ nhất thiên sơn\" của vùng đất phương Nam trù phú. Quần thể núi non kỳ vĩ này không chỉ sở hữu cảnh sắc thiên nhiên tuyệt mỹ bốn mùa mây phủ, mà còn là chốn linh thiêng gắn liền với huyền tích sâu sắc về Linh Sơn Thánh Mẫu kiên trinh, giàu lòng nhân ái. Đặt chân lên đỉnh thiêng, du khách sẽ được chiêm ngưỡng tượng Phật Bà Tây Bổ Đà Sơn uy nghiêm giữa biển mây và đắm mình trong không gian thiền định thoát tục.\n\n" +
                   "\"Linh sơn sừng sững giữa trời Nam — Mây vờn đỉnh biếc, tâm an chốn thiền.\"\n\n" +
                   "Hơi thở non ngàn thanh khiết nơi đây như gột rửa mọi ưu tư trần thế, mở ra cho du khách một hành trình chiêm bái trọn vẹn và an lạc sâu thẳm trong tâm hồn.";
        }
        else if (lower.Contains("bạch đằng") || lower.Contains("bach dang"))
        {
            text = "Bến Bạch Đằng tọa lạc bên bờ sông Sài Gòn thơ mộng, là một trong những địa danh lịch sử và biểu tượng văn hóa tiêu biểu của Thành phố Hồ Chí Minh. Nơi đây gắn liền với những mốc son hào hùng trong công cuộc dựng nước và giữ nước của dân tộc, tiêu biểu là các chiến công lẫy lừng trên dòng sông Bạch Đằng lịch sử và quá trình hình thành, phát triển đô thị Sài Gòn - Gia Định qua hơn ba thế kỷ. Bước chân đến bến cảng hôm nay, du khách vừa cảm nhận được hơi thở thiêng liêng của quá khứ, vừa phóng tầm mắt ngắm nhìn nhịp sống hiện đại vươn mình của đại đô thị phương Nam.\n\n" +
                   "\"Sóng nước Bạch Đằng ghi dấu tích anh hùng — Đất thép nở hoa, phố thị bừng sáng.\"\n\n" +
                   "Dòng sông hiền hòa vẫn ngày đêm cuộn chảy như mạch nguồn bất tận kết nối truyền thống kiên cường với khát vọng tương lai rạng rỡ của đất nước.";
        }
        else if (lower.Contains("núi") || lower.Contains("đèo") || lower.Contains("thác") || lower.Contains("đồi"))
        {
            text = $"{poiName} sừng sững giữa không gian khoáng đạt của đất trời, là một trong những kỳ quan thiên nhiên và thắng cảnh nổi tiếng thu hút đông đảo du khách thập phương. Địa danh này nổi bật bởi địa thế non cao hiểm trở, thảm thực vật xanh tươi và khí hậu mát lành quanh năm, gắn liền với những câu chuyện huyền thoại của cư dân bản địa qua bao thế hệ dựng xây bờ cõi.\n\n" +
                   $"\"{poiName} cao vời vợi chạm tầng mây — Khí phách non ngàn hun đúc hồn non nước.\"\n\n" +
                   "Đứng trước khung cảnh kỳ vĩ của núi non mây ngàn nơi đây, du khách sẽ cảm nhận được sự thanh thản, trút bỏ mọi muộn phiền để hòa mình trọn vẹn vào hơi thở tinh khôi của đất mẹ thiên nhiên.";
        }
        else if (lower.Contains("sông") || lower.Contains("hồ") || lower.Contains("bến") || lower.Contains("biển") || lower.Contains("vịnh"))
        {
            text = $"{poiName} là điểm đến sông nước thanh bình mang đậm dấu ấn phong cảnh hữu tình và đời sống văn hóa bản địa truyền thống. Nơi đây từng là bến nước chở nặng ân tình của nhiều thế hệ cư dân và chứng kiến biết bao biến thiên thăng trầm của lịch sử vùng đất, mang lại cho du khách cảm giác thư thái vô ngần.\n\n" +
                   $"\"{poiName} mênh mang dòng chảy ân tình — Ngàn năm bồi đắp phù sa và nguồn cội.\"\n\n" +
                   "Lắng nghe tiếng sóng nước vỗ về mạn thuyền, du khách sẽ thấu hiểu sâu sắc hơn mối giao hòa bền chặt giữa con người với dòng sông bến nước quê hương.";
        }
        else if (lower.Contains("chùa") || lower.Contains("đền") || lower.Contains("miếu") || lower.Contains("đình") || lower.Contains("nhà thờ"))
        {
            text = $"{poiName} là chốn thiêng liêng giàu truyền thống tín ngưỡng và di sản tâm linh của nhân dân địa phương. Ngôi cổ tự sở hữu nét kiến trúc tinh xảo với mái ngói cong vút, những hàng cột gỗ vững chãi cùng hệ thống tượng thờ, hoành phi câu đối cổ kính nhuốm màu thời gian, là điểm tựa tinh thần gửi gắm ước nguyện bình an.\n\n" +
                   $"\"{poiName} chốn thanh tịnh lắng hồn người — Chuông sớm chiều ru an lạc tâm can.\"\n\n" +
                   "Bước chân vào khuôn viên u tịch thoang thoảng khói trầm, du khách sẽ tìm thấy sự tĩnh tại, an yên và thanh lọc tâm hồn giữa nhịp sống hối hả thường nhật.";
        }
        else
        {
            text = $"{poiName} là điểm đến văn hóa và di sản tiêu biểu thuộc {areaName}, kết tinh những giá trị tinh thần quý báu và cảnh quan đặc sắc qua từng chặng đường phát triển. Bước chân đến không gian nơi đây, du khách sẽ được lắng nghe những câu chuyện lịch sử sâu lắng, cảm nhận vẻ đẹp hài hòa của kiến trúc và thưởng lãm vẻ đẹp văn hóa giàu bản sắc do các thế hệ tiền nhân trao truyền.\n\n" +
                   $"\"{poiName} — Nơi lắng đọng hồn thiêng non nước và chiều sâu văn hóa trường tồn.\"\n\n" +
                   "Hành trình trải nghiệm tại điểm dừng này chắc chắn sẽ để lại trong lòng du khách những dấu ấn tốt đẹp và tình yêu sâu đậm đối với di sản văn hóa dân tộc.";
        }

        return $"[Bản thảo AI] {text}";
    }
}
