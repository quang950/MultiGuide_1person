using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiGuide.Api.Auth;

namespace MultiGuide.Api.Controllers;

/// <summary>
/// Base controller cho các endpoint quản trị (/admin/*), yêu cầu JWT Supabase hợp lệ và role=admin (US-09).
/// </summary>
[ApiController]
[Authorize(Policy = JwtAuthenticationExtensions.AdminPolicyName)]
[Route("admin/[controller]")]
[Route("api/v1/admin/[controller]")]
[Produces("application/json", "application/problem+json")]
public abstract class AdminBaseController : ControllerBase
{
}
