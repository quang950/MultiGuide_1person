using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace MultiGuide.Api.Auth;

/// <summary>
/// Xử lý kiểm tra quyền admin từ JWT của Supabase.
/// Hỗ trợ claim role trực tiếp, ClaimTypes.Role, và JSON payload trong app_metadata / user_metadata.
/// </summary>
public class AdminRoleHandler : AuthorizationHandler<AdminRoleRequirement>
{
    private const string AdminRole = "admin";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminRoleRequirement requirement)
    {
        // 1. Kiểm tra trực tiếp claim 'role' hoặc ClaimTypes.Role
        if (context.User.HasClaim(c =>
            (c.Type == "role" || c.Type == ClaimTypes.Role) &&
            string.Equals(c.Value, AdminRole, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Kiểm tra trong Supabase app_metadata: {"role": "admin", ...}
        var appMetadataClaim = context.User.FindFirst("app_metadata");
        if (appMetadataClaim != null && !string.IsNullOrWhiteSpace(appMetadataClaim.Value))
        {
            if (HasRoleInMetadata(appMetadataClaim.Value, AdminRole))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        // 3. Dự phòng kiểm tra trong user_metadata
        var userMetadataClaim = context.User.FindFirst("user_metadata");
        if (userMetadataClaim != null && !string.IsNullOrWhiteSpace(userMetadataClaim.Value))
        {
            if (HasRoleInMetadata(userMetadataClaim.Value, AdminRole))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        return Task.CompletedTask;
    }

    private static bool HasRoleInMetadata(string jsonMetadata, string expectedRole)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonMetadata);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (doc.RootElement.TryGetProperty("role", out var roleElement) &&
                roleElement.ValueKind == JsonValueKind.String)
            {
                return string.Equals(roleElement.GetString(), expectedRole, StringComparison.OrdinalIgnoreCase);
            }

            // Hỗ trợ trường hợp roles là một mảng chuỗi: {"roles": ["admin", ...]}
            if (doc.RootElement.TryGetProperty("roles", out var rolesElement) &&
                rolesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in rolesElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String &&
                        string.Equals(item.GetString(), expectedRole, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Payload không phải JSON hợp lệ, bỏ qua
        }

        return false;
    }
}
