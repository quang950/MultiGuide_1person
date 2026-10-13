namespace MultiGuide.Domain.Entities;

public class PlayLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PassId { get; set; }
    public Guid PoiId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public int ListenedSec { get; set; }
    public bool Completed { get; set; }

    // Navigation properties
    public AccessPass? Pass { get; set; }
    public Poi? Poi { get; set; }
    public Language? Language { get; set; }
}
