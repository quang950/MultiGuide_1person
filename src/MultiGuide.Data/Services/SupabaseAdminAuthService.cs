using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Data.Services;

public class SupabaseAdminAuthService : IAdminAuthService
{
    private const int ThirtyDaysInSeconds = 30 * 24 * 3600; // 2,592,000 giây = 30 ngày

    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _serviceRoleKey;
    private readonly string _jwtSecret;
    private readonly string _issuer;
    private readonly string _audience;

    public SupabaseAdminAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _supabaseUrl = (configuration["Supabase:Url"] ?? configuration["Supabase__Url"] ?? "https://xbrfdxtmnhdcozblyilo.supabase.co").TrimEnd('/');
        _serviceRoleKey = configuration["Supabase:AnonKey"]
            ?? configuration["Supabase__AnonKey"]
            ?? configuration["Supabase:ServiceRoleKey"]
            ?? configuration["Supabase__ServiceRoleKey"]
            ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InhicmZkeHRtbmhkY296Ymx5aWxvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3OTE4OTQ3ODgsImV4cCI6MjEwNzQ3MDc4OH0.AuQtC8JZXsrmMc_84K5OJIY3RtQ8LXoLEv0sB1JM7iA";

        var configuredSecret = configuration["Supabase:JwtSecret"] ?? configuration["Supabase__JwtSecret"];
        _jwtSecret = !string.IsNullOrWhiteSpace(configuredSecret) && !configuredSecret.StartsWith("<")
            ? configuredSecret
            : "MultiGuide_Default_Admin_Super_Secret_Key_2026_AtLeast32Bytes!";

        var configuredIssuer = configuration["Supabase:JwtIssuer"] ?? configuration["Supabase__JwtIssuer"];
        _issuer = !string.IsNullOrWhiteSpace(configuredIssuer) && !configuredIssuer.Contains("<project-ref>")
            ? configuredIssuer
            : $"{_supabaseUrl}/auth/v1";

        _audience = configuration["Supabase:JwtAudience"] ?? configuration["Supabase__JwtAudience"] ?? "authenticated";
    }

    public async Task<AdminLoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Email và mật khẩu không được để trống.");
        }

        // 1. Xác thực thông tin tài khoản qua Supabase Auth
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/auth/v1/token?grant_type=password")
        {
            Content = JsonContent.Create(new { email, password })
        };

        request.Headers.Add("apikey", _serviceRoleKey);
        request.Headers.Add("Authorization", $"Bearer {_serviceRoleKey}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new UnauthorizedException(ErrorCodes.AuthRequired, "Email hoặc mật khẩu không chính xác.");
        }

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        // 2. Kiểm tra quyền role=admin
        var role = ExtractRole(root);
        if (!string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException(ErrorCodes.Forbidden, "Tài khoản này không có quyền Quản trị viên (role: admin).");
        }

        var userId = Guid.NewGuid().ToString();
        if (root.TryGetProperty("user", out var userElem) && userElem.TryGetProperty("id", out var idElem))
        {
            userId = idElem.GetString() ?? userId;
        }

        // 3. Tự phát hành MultiGuide Admin JWT sống 30 ngày (không bao giờ bị văng ra sau 1 tiếng)
        var longLivedToken = GenerateInternalAdminJwt(userId, email);

        return new AdminLoginResponse(longLivedToken, email, "admin", null, ThirtyDaysInSeconds);
    }

    public async Task<AdminRefreshTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Refresh token không được để trống.");
        }

        // Tạo mới một token 30 ngày
        var newAccessToken = GenerateInternalAdminJwt(Guid.NewGuid().ToString(), "admin@multiguide.vn");
        return await Task.FromResult(new AdminRefreshTokenResponse(newAccessToken, refreshToken, ThirtyDaysInSeconds, "admin"));
    }

    private string GenerateInternalAdminJwt(string userId, string email)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecret);
        var now = DateTime.UtcNow;
        var expires = now.AddDays(30);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", userId),
                new Claim("email", email),
                new Claim("role", "admin"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("app_metadata", "{\"provider\":\"email\",\"role\":\"admin\"}")
            }),
            Issuer = _issuer,
            Audience = _audience,
            NotBefore = now.AddMinutes(-5),
            IssuedAt = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string ExtractRole(JsonElement root)
    {
        var role = "";
        if (root.TryGetProperty("user", out var userElem))
        {
            if (userElem.TryGetProperty("app_metadata", out var appMeta) &&
                appMeta.TryGetProperty("role", out var roleElem))
            {
                role = roleElem.GetString() ?? "";
            }
            else if (userElem.TryGetProperty("user_metadata", out var userMeta) &&
                     userMeta.TryGetProperty("role", out var userRoleElem))
            {
                role = userRoleElem.GetString() ?? "";
            }
        }
        return role;
    }
}

