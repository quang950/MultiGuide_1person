namespace MultiGuide.Business.DTOs.Client;

public class AreaDetailResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public double RadiusM { get; set; }
    public decimal PriceUsd { get; set; }
    public int PassTtlHours { get; set; }
    public IReadOnlyList<SupportedLanguageDto> SupportedLanguages { get; set; } = [];
}

public class SupportedLanguageDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class AreaSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double CenterLat { get; set; }
    public double CenterLng { get; set; }
    public double RadiusM { get; set; }
    public decimal PriceUsd { get; set; }
    public int PassTtlHours { get; set; }
    public double? DistanceMeters { get; set; }
}

public class AreaPoiSummaryDto
{
    public Guid Id { get; set; }
    public int Order { get; set; }
    public int Priority { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleEn { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double TriggerRadiusM { get; set; }
    public string? ImageUrl { get; set; }
    public string? Summary { get; set; }
}
