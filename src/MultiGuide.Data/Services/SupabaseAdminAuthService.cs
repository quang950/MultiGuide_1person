using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Data.Services;

public class SupabaseAdminAuthService : IAdminAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _serviceRoleKey;

    public SupabaseAdminAuthService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _supabaseUrl = (configuration["Supabase:Url"] ?? configuration["Supabase__Url"] ?? "https://xbrfdxtmnhdcozblyilo.supabase.co").TrimEnd('/');
        _serviceRoleKey = configuration["Supabase:AnonKey"]
            ?? configuration["Supabase__AnonKey"]
            ?? configuration["Supabase:ServiceRoleKey"]
            ?? configuration["Supabase__ServiceRoleKey"]
            ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InhicmZkeHRtbmhkY296Ymx5aWxvIiwicm9sZSI6ImFub24iLCJpYXQiOjE3OTE4OTQ3ODgsImV4cCI6MjEwNzQ3MDc4OH0.AuQtC8JZXsrmMc_84K5OJIY3RtQ8LXoLEv0sB1JM7iA";
    }

    public async Task<AdminLoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Email và mật khẩu không được để trống.");
        }

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
        var token = root.GetProperty("access_token").GetString() ?? string.Empty;

        // Kiểm tra claim role=admin trong app_metadata hoặc user_metadata
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

        if (!string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException(ErrorCodes.Forbidden, "Tài khoản này không có quyền Quản trị viên (role: admin).");
        }

        return new AdminLoginResponse(token, email, "admin");
    }
}
