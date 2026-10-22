namespace MultiGuide.Business.DTOs.Admin;

public record DashboardAnalyticsResponse
{
    public int TotalAreas { get; init; }
    public int TotalPois { get; init; }
    public int TotalPasses { get; init; }
    public decimal TotalRevenueUsd { get; init; }
    public int TotalPlayLogs { get; init; }
    public int TotalListenedSec { get; init; }
    public int ActiveVisitorsCount { get; init; }
    public IReadOnlyList<PoiStatDto> TopPois { get; init; } = Array.Empty<PoiStatDto>();
    public IReadOnlyList<LanguageStatDto> LanguageDistribution { get; init; } = Array.Empty<LanguageStatDto>();
}

public record PoiStatDto(
    Guid PoiId,
    string PoiName,
    string AreaName,
    int PlayCount,
    int TotalListenedSec,
    int CompletedCount
);

public record LanguageStatDto(
    string LanguageCode,
    string LanguageName,
    int PlayCount,
    double Percentage
);
