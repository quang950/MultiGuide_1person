namespace MultiGuide.Domain.Entities;

public class Language
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<PoiTranslation> Translations { get; set; } = new List<PoiTranslation>();
    public ICollection<PlayLog> PlayLogs { get; set; } = new List<PlayLog>();
}
