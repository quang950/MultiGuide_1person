namespace MultiGuide.Business.DTOs.Admin;

public class CreateTranslationRequest
{
    public string LanguageCode { get; set; } = string.Empty;
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
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AudioPath { get; set; }
    public int? AudioDurationSec { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GenerateAudioRequest
{
    public string? Text { get; set; }
}

public class GenerateAudioResponse
{
    public Guid TranslationId { get; set; }
    public string AudioPath { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public int AudioSizeBytes { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
}

