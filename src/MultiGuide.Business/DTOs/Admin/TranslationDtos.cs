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
