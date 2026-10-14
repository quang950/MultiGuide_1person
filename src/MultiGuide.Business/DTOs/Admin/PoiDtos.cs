namespace MultiGuide.Business.DTOs.Admin;

public class CreatePoiRequest
{
    public Guid AreaId { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double TriggerRadiusM { get; set; } = 30;
    public int Priority { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdatePoiRequest
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double TriggerRadiusM { get; set; } = 30;
    public int Priority { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PoiResponse
{
    public Guid Id { get; set; }
    public Guid AreaId { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double TriggerRadiusM { get; set; }
    public int Priority { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
