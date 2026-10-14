namespace MultiGuide.Business.DTOs.Admin;

public class CreateAreaRequest
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public double RadiusM { get; set; }
    public decimal PriceUsd { get; set; }
    public int PassTtlHours { get; set; } = 24;
}

public class UpdateAreaRequest
{
    public string Name { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public double RadiusM { get; set; }
    public decimal PriceUsd { get; set; }
    public int PassTtlHours { get; set; } = 24;
    public bool IsActive { get; set; } = true;
}

public class AreaResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public double RadiusM { get; set; }
    public decimal PriceUsd { get; set; }
    public int PassTtlHours { get; set; }
    public bool IsActive { get; set; }
}
