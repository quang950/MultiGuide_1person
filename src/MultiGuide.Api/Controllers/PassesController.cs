using Microsoft.AspNetCore.Mvc;
using MultiGuide.Api.Auth;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Api.Controllers;

/// <summary>
/// Quản lý trạng thái và thông tin vé AccessPass của khách (US-04).
/// </summary>
[Route("passes")]
[Route("api/v1/passes")]
public class PassesController : BaseApiController
{
    private readonly IPassService _passService;

    public PassesController(IPassService passService)
    {
        _passService = passService;
    }

    /// <summary>
    /// Lấy thông tin trạng thái và thời hạn hiệu lực của vé AccessPass hiện tại (US-04).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Thông tin vé AccessPass</returns>
    [HttpGet("current")]
    [RequireAccessPass]
    [ProducesResponseType(typeof(CurrentPassResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentPassResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        var passContext = HttpContext.GetPassContext();
        if (passContext == null)
        {
            throw new UnauthorizedException(ErrorCodes.PassMissing, "Yêu cầu AccessPass hợp lệ.");
        }

        var pass = await _passService.GetCurrentPassAsync(passContext.PassId, cancellationToken);
        return Ok(pass);
    }
}
