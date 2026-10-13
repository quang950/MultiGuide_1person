using Microsoft.AspNetCore.Authorization;

namespace MultiGuide.Api.Auth;

/// <summary>
/// Yêu cầu người dùng phải có vai trò admin (thông qua claim role hoặc Supabase app_metadata.role).
/// </summary>
public class AdminRoleRequirement : IAuthorizationRequirement
{
}
