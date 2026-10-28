using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Services;

namespace MultiGuide.Api.Controllers;

/// <summary>
/// Xác thực đăng nhập Quản trị viên qua Supabase Auth bảo mật (Zero-Exposure).
/// </summary>
[ApiController]
[Route("admin/auth")]
[Route("api/v1/admin/auth")]
[Produces("application/json", "application/problem+json")]
public class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthService _adminAuthService;

    public AdminAuthController(IAdminAuthService adminAuthService)
    {
        _adminAuthService = adminAuthService;
    }

    /// <summary>
    /// Đăng nhập tài khoản Quản trị viên (không để lộ API key ra client).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AdminLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AdminLoginResponse>> Login(
        [FromBody] AdminLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminAuthService.LoginAsync(request.Email, request.Password, cancellationToken);
        return Ok(result);
    }
}
