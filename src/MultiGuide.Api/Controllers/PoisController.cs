using Microsoft.AspNetCore.Mvc;
using MultiGuide.Api.Auth;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Api.Controllers;

/// <summary>
/// Điểm tham quan du lịch cho khách (US-05, US-06, US-07).
/// </summary>
[Route("pois")]
[Route("api/v1/pois")]
public class PoisController : BaseApiController
{
    private readonly IPoiService _poiService;

    public PoisController(IPoiService poiService)
    {
        _poiService = poiService;
    }

    /// <summary>
    /// Dò tìm các điểm tham quan lân cận toạ độ GPS của khách tham quan trong khu vực của vé AccessPass (US-05).
    /// </summary>
    /// <param name="lat">Vĩ độ GPS của khách (khoảng [-90, 90])</param>
    /// <param name="lng">Kinh độ GPS của khách (khoảng [-180, 180])</param>
    /// <param name="radius">Bán kính quét theo mét (mặc định 100m, tối đa 500m)</param>
    /// <param name="lang">Mã ngôn ngữ mong muốn (fallback: en -> vi)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Danh sách POI trong bán kính, kèm khoảng cách và cờ within_trigger, sắp xếp theo ưu tiên và khoảng cách</returns>
    [HttpGet("nearby")]
    [RequireAccessPass]
    [ProducesResponseType(typeof(IReadOnlyList<NearbyPoiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<IReadOnlyList<NearbyPoiResponse>>> GetNearby(
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] double? radius,
        [FromQuery] string? lang,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!lat.HasValue)
        {
            errors["lat"] = ["Tham số 'lat' là bắt buộc."];
        }
        else if (lat.Value < -90.0 || lat.Value > 90.0)
        {
            errors["lat"] = ["Vĩ độ 'lat' phải nằm trong khoảng [-90, 90]."];
        }

        if (!lng.HasValue)
        {
            errors["lng"] = ["Tham số 'lng' là bắt buộc."];
        }
        else if (lng.Value < -180.0 || lng.Value > 180.0)
        {
            errors["lng"] = ["Kinh độ 'lng' phải nằm trong khoảng [-180, 180]."];
        }

        var effectiveRadius = radius ?? 100.0;
        if (effectiveRadius <= 0.0 || effectiveRadius > 500.0)
        {
            errors["radius"] = ["Bán kính 'radius' phải lớn hơn 0 và tối đa là 500 mét."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Dữ liệu toạ độ hoặc bán kính không hợp lệ.", errors);
        }

        var passContext = HttpContext.GetPassContext();
        if (passContext == null)
        {
            throw new UnauthorizedException(ErrorCodes.PassMissing, "Yêu cầu AccessPass hợp lệ.");
        }

        var pois = await _poiService.GetNearbyPoisAsync(
            passContext.AreaId,
            lat!.Value,
            lng!.Value,
            effectiveRadius,
            lang,
            cancellationToken);

        return Ok(pois);
    }
}
