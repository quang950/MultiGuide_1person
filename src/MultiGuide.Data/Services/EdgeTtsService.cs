using System.Net.WebSockets;
using System.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Data.Services;

/// <summary>
/// Hiện thực ITtsService sử dụng Microsoft Edge Neural Text-to-Speech qua WebSocket.
/// Cung cấp giọng đọc bản xứ tự nhiên, không cần API key và hỗ trợ đa ngôn ngữ (vi, en, ja, zh).
/// </summary>
public class EdgeTtsService : ITtsService
{
    private const string WsEndpoint = "wss://speech.platform.bing.com/consumer/speech/synthesize/readaheadedge/v1";
    private const string TrustedClientToken = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";

    private readonly ILogger<EdgeTtsService> _logger;

    public EdgeTtsService(ILogger<EdgeTtsService> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> SynthesizeSpeechAsync(
        string text,
        string languageCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Văn bản cần đọc không được để trống.");
        }

        var voice = ResolveVoice(languageCode);
        var connectionId = Guid.NewGuid().ToString("N");
        var uri = new Uri($"{WsEndpoint}?TrustedClientToken={TrustedClientToken}&ConnectionId={connectionId}");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        using var ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Pragma", "no-cache");
        ws.Options.SetRequestHeader("Cache-Control", "no-cache");
        ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.0.0");
        ws.Options.SetRequestHeader("Origin", "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");

        try
        {
            _logger.LogInformation("Connecting to Edge Neural TTS for voice {Voice}...", voice);
            await ws.ConnectAsync(uri, cts.Token);

            // 1. Gửi cấu hình định dạng âm thanh (audio-24khz-48kbitrate-mono-mp3)
            var configPayload = "Content-Type:application/json; charset=utf-8\r\nPath:speech.config\r\n\r\n" +
                "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{\"sentenceBoundaryEnabled\":\"false\",\"wordBoundaryEnabled\":\"false\"},\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"}}}}";
            await ws.SendAsync(Encoding.UTF8.GetBytes(configPayload), WebSocketMessageType.Text, true, cts.Token);

            // 2. Gửi kịch bản SSML
            var requestId = Guid.NewGuid().ToString("N");
            var escapedText = SecurityElement.Escape(text.Trim());
            var ssml = $"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{languageCode}'><voice name='{voice}'>{escapedText}</voice></speak>";
            var ssmlPayload = $"X-RequestId:{requestId}\r\nContent-Type:application/ssml+xml\r\nPath:ssml\r\n\r\n{ssml}";
            await ws.SendAsync(Encoding.UTF8.GetBytes(ssmlPayload), WebSocketMessageType.Text, true, cts.Token);

            // 3. Đọc dữ liệu âm thanh từ server trả về
            using var audioStream = new MemoryStream();
            var receiveBuffer = new byte[8192];
            using var messageBuffer = new MemoryStream();

            while (ws.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                WebSocketReceiveResult result;
                messageBuffer.SetLength(0);

                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                    messageBuffer.Write(receiveBuffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                var chunk = messageBuffer.ToArray();

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var headerText = Encoding.UTF8.GetString(chunk);
                    if (headerText.Contains("Path:turn.end", StringComparison.OrdinalIgnoreCase))
                    {
                        // Hoàn tất luồng phát âm thanh
                        break;
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Binary && chunk.Length > 2)
                {
                    // Định dạng gói tin nhị phân Edge TTS: 2 bytes header length + Header text + Raw MP3 audio data
                    int headerLen = (chunk[0] << 8) | chunk[1];
                    int audioStartIndex = 2 + headerLen;
                    if (chunk.Length > audioStartIndex)
                    {
                        audioStream.Write(chunk, audioStartIndex, chunk.Length - audioStartIndex);
                    }
                }
            }

            var audioBytes = audioStream.ToArray();
            if (audioBytes.Length == 0)
            {
                throw new ValidationException(ErrorCodes.ValidationError, "Dịch vụ Edge TTS không trả về dữ liệu âm thanh.");
            }

            _logger.LogInformation("Edge Neural TTS synthesized {ByteCount} bytes of MP3 successfully for {Voice}.", audioBytes.Length, voice);
            return audioBytes;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Edge Neural TTS request timed out or was cancelled.");
            throw new ValidationException(ErrorCodes.ValidationError, "Quá trình sinh giọng đọc âm thanh bị quá thời gian chờ (timeout). Vui lòng thử lại.");
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _logger.LogError(ex, "Error synthesizing speech with Edge TTS: {Message}", ex.Message);
            throw new ValidationException(ErrorCodes.ValidationError, $"Lỗi kết nối dịch vụ giọng đọc Neural TTS: {ex.Message}");
        }
        finally
        {
            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Completed", CancellationToken.None);
                }
                catch
                {
                    // Ignore close exceptions
                }
            }
        }
    }

    private static string ResolveVoice(string languageCode)
    {
        return (languageCode ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "vi" => "vi-VN-HoaiMyNeural",
            "en" => "en-US-JennyNeural",
            "ja" => "ja-JP-NanamiNeural",
            "zh" => "zh-CN-XiaoxiaoNeural",
            _ => "vi-VN-HoaiMyNeural"
        };
    }
}
