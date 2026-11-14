using System.Text.Json.Serialization;

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

    [JsonPropertyName("center_lat")]
    public double? CenterLatSnakeCase { get => CenterLat; set { if (value.HasValue) CenterLat = value.Value; } }

    [JsonPropertyName("center_lng")]
    public double? CenterLngSnakeCase { get => CenterLng; set { if (value.HasValue) CenterLng = value.Value; } }

    [JsonPropertyName("radius_m")]
    public double? RadiusMSnakeCase { get => RadiusM; set { if (value.HasValue) RadiusM = value.Value; } }

    [JsonPropertyName("price_usd")]
    public decimal? PriceUsdSnakeCase { get => PriceUsd; set { if (value.HasValue) PriceUsd = value.Value; } }

    [JsonPropertyName("pass_ttl_hours")]
    public int? PassTtlHoursSnakeCase { get => PassTtlHours; set { if (value.HasValue) PassTtlHours = value.Value; } }
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

    [JsonPropertyName("center_lat")]
    public double? CenterLatSnakeCase { get => CenterLat; set { if (value.HasValue) CenterLat = value.Value; } }

    [JsonPropertyName("center_lng")]
    public double? CenterLngSnakeCase { get => CenterLng; set { if (value.HasValue) CenterLng = value.Value; } }

    [JsonPropertyName("radius_m")]
    public double? RadiusMSnakeCase { get => RadiusM; set { if (value.HasValue) RadiusM = value.Value; } }

    [JsonPropertyName("price_usd")]
    public decimal? PriceUsdSnakeCase { get => PriceUsd; set { if (value.HasValue) PriceUsd = value.Value; } }

    [JsonPropertyName("pass_ttl_hours")]
    public int? PassTtlHoursSnakeCase { get => PassTtlHours; set { if (value.HasValue) PassTtlHours = value.Value; } }

    [JsonPropertyName("is_active")]
    public bool? IsActiveSnakeCase { get => IsActive; set { if (value.HasValue) IsActive = value.Value; } }
}

public class UpdateAreaStatusRequest
{
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("is_active")]
    public bool? IsActiveSnakeCase { get => IsActive; set { if (value.HasValue) IsActive = value.Value; } }
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

    [JsonPropertyName("center_lat")]
    public double CenterLatSnakeCase => CenterLat;

    [JsonPropertyName("center_lng")]
    public double CenterLngSnakeCase => CenterLng;

    [JsonPropertyName("radius_m")]
    public double RadiusMSnakeCase => RadiusM;

    [JsonPropertyName("price_usd")]
    public decimal PriceUsdSnakeCase => PriceUsd;

    [JsonPropertyName("pass_ttl_hours")]
    public int PassTtlHoursSnakeCase => PassTtlHours;

    [JsonPropertyName("is_active")]
    public bool IsActiveSnakeCase => IsActive;
}
