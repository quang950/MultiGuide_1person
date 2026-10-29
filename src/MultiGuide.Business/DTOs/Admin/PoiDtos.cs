using System.Text.Json.Serialization;

namespace MultiGuide.Business.DTOs.Admin;

public class CreatePoiRequest
{
    public Guid AreaId { get; set; }

    [JsonPropertyName("area_id")]
    public Guid? AreaIdSnakeCase
    {
        set
        {
            if (value.HasValue && value.Value != Guid.Empty)
            {
                AreaId = value.Value;
            }
        }
    }

    public double Lat { get; set; }
    public double Lng { get; set; }
    public double TriggerRadiusM { get; set; } = 30;

    [JsonPropertyName("trigger_radius_m")]
    public double? TriggerRadiusMSnakeCase
    {
        set
        {
            if (value.HasValue)
            {
                TriggerRadiusM = value.Value;
            }
        }
    }

    public int Priority { get; set; }
    public string? ImageUrl { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrlSnakeCase
    {
        set
        {
            if (value != null)
            {
                ImageUrl = value;
            }
        }
    }
}

public class UpdatePoiRequest
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public double TriggerRadiusM { get; set; } = 30;

    [JsonPropertyName("trigger_radius_m")]
    public double? TriggerRadiusMSnakeCase
    {
        set
        {
            if (value.HasValue)
            {
                TriggerRadiusM = value.Value;
            }
        }
    }

    public int Priority { get; set; }
    public string? ImageUrl { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrlSnakeCase
    {
        set
        {
            if (value != null)
            {
                ImageUrl = value;
            }
        }
    }

    public bool IsActive { get; set; } = true;

    [JsonPropertyName("is_active")]
    public bool? IsActiveSnakeCase
    {
        set
        {
            if (value.HasValue)
            {
                IsActive = value.Value;
            }
        }
    }
}

public class PoiResponse
{
    public Guid Id { get; set; }
    public Guid AreaId { get; set; }

    [JsonPropertyName("area_id")]
    public Guid AreaIdSnakeCase => AreaId;

    public double Lat { get; set; }
    public double Lng { get; set; }
    public double TriggerRadiusM { get; set; }

    [JsonPropertyName("trigger_radius_m")]
    public double TriggerRadiusMSnakeCase => TriggerRadiusM;

    public int Priority { get; set; }
    public string? ImageUrl { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrlSnakeCase => ImageUrl;

    public bool IsActive { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActiveSnakeCase => IsActive;

    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAtSnakeCase => CreatedAt;

    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAtSnakeCase => UpdatedAt;
}

public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
