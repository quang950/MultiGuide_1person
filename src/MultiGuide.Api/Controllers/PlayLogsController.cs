using Microsoft.AspNetCore.Mvc;
using MultiGuide.Api.Auth;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Api.Controllers;

/// <summary>
/// Ghi nhận nhật ký nghe audio thuyết minh của khách tham quan (US-08).
/// </summary>
[Route("play-logs")]
[Route("api/v1/play-logs")]
public class PlayLogsController : BaseApiController
{
    private readonly IPlayLogService _playLogService;

    public PlayLogsController(IPlayLogService playLogService)
    {
        _playLogService = playLogService;
    }

    /// <summary>
    /// Ghi nhận nhật ký nghe thuyết minh âm thanh cho một điểm tham quan (US-08).
    /// </summary>
    /// <param name="request">Thông tin lượt nghe audio (poi_id, language_code, listened_sec, completed)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Kết quả bản ghi nhật ký nghe</returns>
    [HttpPost]
    [RequireAccessPass]
    [ProducesResponseType(typeof(PlayLogResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PlayLogResponse>> CreatePlayLog(
        [FromBody] CreatePlayLogRequest request,
        CancellationToken cancellationToken)
    {
        var passContext = HttpContext.GetPassContext();
        if (passContext == null)
        {
            throw new UnauthorizedException(ErrorCodes.PassMissing, "Yêu cầu AccessPass hợp lệ.");
        }

        var result = await _playLogService.CreatePlayLogAsync(
            passContext.PassId,
            passContext.AreaId,
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
