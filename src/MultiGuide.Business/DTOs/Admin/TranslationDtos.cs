using System.Text.Json.Serialization;

namespace MultiGuide.Business.DTOs.Admin;

public class CreateTranslationRequest
{
    public string LanguageCode { get; set; } = string.Empty;

    [JsonPropertyName("language_code")]
    public string? LanguageCodeSnakeCase
    {
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                LanguageCode = value;
            }
        }
    }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class UpdateTranslationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class TranslationResponse
{
    public Guid Id { get; set; }
    public Guid PoiId { get; set; }

    [JsonPropertyName("poi_id")]
    public Guid PoiIdSnakeCase => PoiId;

    public string LanguageCode { get; set; } = string.Empty;

    [JsonPropertyName("language_code")]
    public string LanguageCodeSnakeCase => LanguageCode;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AudioPath { get; set; }

    [JsonPropertyName("audio_path")]
    public string? AudioPathSnakeCase => AudioPath;

    public int? AudioDurationSec { get; set; }

    [JsonPropertyName("audio_duration_sec")]
    public int? AudioDurationSecSnakeCase => AudioDurationSec;

    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAtSnakeCase => UpdatedAt;
}

public class GenerateAudioRequest
{
    public string? Text { get; set; }
}

public class GenerateAudioResponse
{
    public Guid TranslationId { get; set; }

    [JsonPropertyName("translation_id")]
    public Guid TranslationIdSnakeCase => TranslationId;

    public string AudioPath { get; set; } = string.Empty;

    [JsonPropertyName("audio_path")]
    public string AudioPathSnakeCase => AudioPath;

    public string AudioUrl { get; set; } = string.Empty;

    [JsonPropertyName("audio_url")]
    public string AudioUrlSnakeCase => AudioUrl;

    public int AudioSizeBytes { get; set; }

    [JsonPropertyName("audio_size_bytes")]
    public int AudioSizeBytesSnakeCase => AudioSizeBytes;

    public string LanguageCode { get; set; } = string.Empty;

    [JsonPropertyName("language_code")]
    public string LanguageCodeSnakeCase => LanguageCode;
}
