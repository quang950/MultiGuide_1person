using System.Diagnostics;
using System.Net.WebSockets;
using System.Security;
using System.Text;
using Microsoft.Extensions.Logging;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Data.Services;

/// <summary>
/// Hiện thực ITtsService sử dụng Microsoft Edge Neural Text-to-Speech qua CLI / Python và WebSocket.
/// Cung cấp giọng đọc bản xứ tự nhiên, không cần API key và hỗ trợ đa ngôn ngữ (vi, en, ja, zh).
/// </summary>
public class EdgeTtsService : ITtsService
{
    private const string WsEndpoint = "wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1";
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

        // 1. Ưu tiên sinh audio qua edge-tts CLI / Python nếu có trên máy (nhanh ~1.5s, ổn định 100%)
        var (cliResult, lastError) = await TrySynthesizeViaCliAsync(text, voice, cancellationToken);
        if (cliResult != null && cliResult.Length > 0)
        {
            return cliResult;
        }

        // 2. Dự phòng: Thông báo lỗi rõ ràng nếu môi trường không thực thi được
        _logger.LogError("Không thể sinh âm thanh Edge Neural TTS: {Error}", lastError);
        throw new ValidationException(
            ErrorCodes.ValidationError,
            $"Không thể kết nối dịch vụ giọng đọc Neural TTS. Chi tiết: {lastError ?? "Vui lòng kiểm tra kết nối mạng và thử lại."}");
    }

    private async Task<(byte[]? Bytes, string? Error)> TrySynthesizeViaCliAsync(string text, string voice, CancellationToken cancellationToken)
    {
        var tempTextFile = Path.Combine(Path.GetTempPath(), $"mg_tts_{Guid.NewGuid():N}.txt");
        var tempOutputFile = Path.Combine(Path.GetTempPath(), $"mg_tts_{Guid.NewGuid():N}.mp3");
        string? lastError = null;

        try
        {
            var sanitizedText = text.Replace("—", ", ").Replace("–", ", ");
            await File.WriteAllTextAsync(tempTextFile, sanitizedText, new UTF8Encoding(false), cancellationToken);

            string? scriptPath = null;
            var explicitPaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "scripts", "tts.py"),
                Path.Combine(AppContext.BaseDirectory, "scripts", "tts.py"),
                @"D:\MultiGuide\scripts\tts.py",
                "/app/scripts/tts.py",
            };

            foreach (var p in explicitPaths)
            {
                if (File.Exists(p))
                {
                    scriptPath = p;
                    break;
                }
            }

            if (scriptPath == null)
            {
                var dir = Directory.GetCurrentDirectory();
                for (int i = 0; i < 6; i++)
                {
                    var check = Path.Combine(dir, "scripts", "tts.py");
                    if (File.Exists(check))
                    {
                        scriptPath = check;
                        break;
                    }
                    var parent = Directory.GetParent(dir);
                    if (parent == null) break;
                    dir = parent.FullName;
                }
            }

            var pythonExecutables = new List<string>
            {
                "/opt/venv/bin/python",
                "/usr/bin/python3",
                "python3",
                @"C:\ProgramData\anaconda3\python.exe",
                "python",
                "py"
            };

            var candidates = new List<(string FileName, string Arguments)>();

            if (!string.IsNullOrEmpty(scriptPath))
            {
                foreach (var py in pythonExecutables)
                {
                    candidates.Add((py, $"\"{scriptPath}\" --text-file \"{tempTextFile}\" --voice \"{voice}\" --rate=-15% --output \"{tempOutputFile}\""));
                }
            }

            candidates.Add(("/opt/venv/bin/edge-tts", $"-f \"{tempTextFile}\" -v \"{voice}\" --rate=-15% --write-media \"{tempOutputFile}\""));
            candidates.Add(("edge-tts", $"-f \"{tempTextFile}\" -v \"{voice}\" --rate=-15% --write-media \"{tempOutputFile}\""));
            foreach (var py in pythonExecutables)
            {
                candidates.Add((py, $"-m edge_tts -f \"{tempTextFile}\" -v \"{voice}\" --rate=-15% --write-media \"{tempOutputFile}\""));
            }

            foreach (var candidate in candidates)
            {
                try
                {
                    using var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = candidate.FileName,
                            Arguments = candidate.Arguments,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8,
                            StandardErrorEncoding = Encoding.UTF8
                        }
                    };

                    proc.Start();
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(60));

                    var stdoutTask = proc.StandardOutput.ReadToEndAsync(cts.Token);
                    var stderrTask = proc.StandardError.ReadToEndAsync(cts.Token);
                    await proc.WaitForExitAsync(cts.Token);

                    var stdout = await stdoutTask;
                    var stderr = await stderrTask;

                    if (proc.ExitCode == 0 && File.Exists(tempOutputFile))
                    {
                        var bytes = await File.ReadAllBytesAsync(tempOutputFile, cancellationToken);
                        if (bytes.Length > 100)
                        {
                            _logger.LogInformation("Successfully synthesized {ByteCount} bytes audio using {Cli}.", bytes.Length, candidate.FileName);
                            return (bytes, null);
                        }
                    }
                    else
                    {
                        lastError = !string.IsNullOrWhiteSpace(stderr) ? stderr.Trim() : stdout.Trim();
                        _logger.LogWarning("TTS candidate {FileName} exited with code {Code}. Stderr: {Stderr}", candidate.FileName, proc.ExitCode, stderr);
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex.Message;
                    _logger.LogDebug(ex, "TTS candidate {FileName} failed or not found.", candidate.FileName);
                }
            }
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            _logger.LogWarning(ex, "TrySynthesizeViaCliAsync error.");
        }
        finally
        {
            try { if (File.Exists(tempTextFile)) File.Delete(tempTextFile); } catch { }
            try { if (File.Exists(tempOutputFile)) File.Delete(tempOutputFile); } catch { }
        }

        return (null, lastError);
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
