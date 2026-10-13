namespace MultiGuide.Domain.Entities;

public class PoiTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PoiId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AudioPath { get; set; }
    public int? AudioDurationSec { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Poi? Poi { get; set; }
    public Language? Language { get; set; }
}
