namespace MultiGuide.Business.DTOs.Client;

/// <summary>
/// Điểm tham quan lân cận vị trí của khách tham quan (US-05).
/// </summary>
public class NearbyPoiResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public int DistanceM { get; set; }
    public bool WithinTrigger { get; set; }
    public double TriggerRadiusM { get; set; }
    public int Priority { get; set; }
    public string? ImageUrl { get; set; }
}
