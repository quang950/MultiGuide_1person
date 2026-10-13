namespace MultiGuide.Domain.Entities;

public class TourArea
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public double RadiusM { get; set; }
    public decimal PriceUsd { get; set; }
    public int PassTtlHours { get; set; } = 24;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Poi> Pois { get; set; } = new List<Poi>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<AccessPass> AccessPasses { get; set; } = new List<AccessPass>();
}
