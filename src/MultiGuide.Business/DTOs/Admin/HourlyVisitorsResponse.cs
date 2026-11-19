namespace MultiGuide.Business.DTOs.Admin;

public record HourlyVisitorsResponse
{
    public int Hours { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public int TotalVisitors { get; init; }
    public IReadOnlyList<HourlyVisitorBucketDto> Buckets { get; init; } = Array.Empty<HourlyVisitorBucketDto>();
}

public record HourlyVisitorBucketDto
{
    public int HoursAgo { get; init; }
    public DateTime WindowStartUtc { get; init; }
    public DateTime WindowEndUtc { get; init; }
    public int VisitorCount { get; init; }
    public IReadOnlyList<HourlyVisitorPassDto> Visitors { get; init; } = Array.Empty<HourlyVisitorPassDto>();
}

public record HourlyVisitorPassDto(
    Guid PassId,
    string PassLabel,
    Guid AreaId,
    string AreaName,
    string AreaCode,
    DateTime IssuedAtUtc,
    DateTime ExpiresAtUtc,
    string Status
);
