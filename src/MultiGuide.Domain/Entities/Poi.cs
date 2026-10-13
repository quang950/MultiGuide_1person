namespace MultiGuide.Domain.Entities;

public class Poi
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AreaId { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double TriggerRadiusM { get; set; } = 30;
    public int Priority { get; set; } = 0;
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public TourArea? Area { get; set; }
    public ICollection<PoiTranslation> Translations { get; set; } = new List<PoiTranslation>();
    public ICollection<PlayLog> PlayLogs { get; set; } = new List<PlayLog>();
}
