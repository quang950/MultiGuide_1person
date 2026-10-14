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
