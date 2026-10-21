using System.Text.Json.Serialization;

namespace MultiGuide.Business.DTOs.Admin;

public record AdminLoginRequest(string Email, string Password);

public record AdminLoginResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken = null,
    [property: JsonPropertyName("expires_in")] int ExpiresIn = 3600);

public record AdminRefreshTokenRequest(
    [property: JsonPropertyName("refresh_token")] string RefreshToken);

public record AdminRefreshTokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("role")] string Role = "admin");
