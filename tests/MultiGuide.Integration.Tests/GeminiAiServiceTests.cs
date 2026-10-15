using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MultiGuide.Data.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Integration.Tests;

public class GeminiAiServiceTests
{
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

    private (GeminiAiService service, MockHttpMessageHandler handler) CreateService(
        string? apiKey = "test-gemini-key",
        string model = "gemini-2.5-flash")
    {
        var mockHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(mockHandler);

        var inMemorySettings = new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = apiKey ?? string.Empty,
            ["Gemini__ApiKey"] = apiKey ?? string.Empty,
            ["Gemini:Model"] = model
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var logger = NullLogger<GeminiAiService>.Instance;
        var service = new GeminiAiService(httpClient, configuration, logger);

        return (service, mockHandler);
    }

    [Fact]
    public async Task GeneratePoiSummaryAsync_HopLe_TraVeNoiDungDungTuGemini()
    {
        var (service, handler) = CreateService();

        var mockGeminiResponse = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  {
                    "text": "Khuê Văn Các là gác chuông nhỏ bằng gỗ dựng năm 1805, một biểu tượng văn hiến nghìn năm của Thăng Long."
                  }
                ],
                "role": "model"
              },
              "finishReason": "STOP"
            }
          ]
        }
        """;

        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(mockGeminiResponse, Encoding.UTF8, "application/json")
        };

        var result = await service.GeneratePoiSummaryAsync("Khuê Văn Các", "Văn Miếu", "vi");

        Assert.NotNull(result);
        Assert.Contains("Khuê Văn Các là gác chuông nhỏ", result);
        Assert.NotNull(handler.LastRequest);
        Assert.Contains("key=test-gemini-key", handler.LastRequest.RequestUri?.Query);
        Assert.Contains("Khuê Văn Các", handler.LastRequestBody);
    }

    [Fact]
    public async Task GeneratePoiSummaryAsync_TiengAnhVaTiengNhat_TaoPromptTuongUng()
    {
        var (service, handler) = CreateService();

        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
            {
              "candidates": [{ "content": { "parts": [{ "text": "Khue Van Pavilion is a historic landmark..." }] } }]
            }
            """, Encoding.UTF8, "application/json")
        };

        var resultEn = await service.GeneratePoiSummaryAsync("Khue Van Pavilion", "Temple of Literature", "en");
        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("tiếng Anh (English)", handler.LastRequestBody);
        Assert.Contains("Khue Van Pavilion is a historic landmark", resultEn);

        var resultJa = await service.GeneratePoiSummaryAsync("Khue Van Pavilion", "Temple of Literature", "ja");
        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("tiếng Nhật (Japanese)", handler.LastRequestBody);
    }

    [Fact]
    public async Task GeneratePoiSummaryAsync_KhongCoApiKey_TraVeBanThaoMoPhong()
    {
        var (service, _) = CreateService(apiKey: null);

        var result = await service.GeneratePoiSummaryAsync("Cổng Văn Miếu", "Văn Miếu", "vi");

        Assert.NotNull(result);
        Assert.Contains("[Bản thảo AI]", result);
        Assert.Contains("Cổng Văn Miếu", result);
    }

    [Fact]
    public async Task GeneratePoiSummaryAsync_GeminiTraVeLoi_NemAppException()
    {
        var (service, handler) = CreateService();

        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\": {\"message\": \"API_KEY_INVALID\"}}", Encoding.UTF8, "application/json")
        };

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.GeneratePoiSummaryAsync("Khuê Văn Các", "Văn Miếu", "vi"));

        Assert.Equal(ErrorCodes.InternalError, ex.Code);
    }

    [Fact]
    public async Task GeneratePoiSummaryAsync_GeminiTraVeCandidatesRong_NemAppException()
    {
        var (service, handler) = CreateService();

        handler.Handler = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"candidates\": []}", Encoding.UTF8, "application/json")
        };

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            service.GeneratePoiSummaryAsync("Khuê Văn Các", "Văn Miếu", "vi"));

        Assert.Equal(ErrorCodes.InternalError, ex.Code);
    }
}
