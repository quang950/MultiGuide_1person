using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MultiGuide.Api.Middleware;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Api.Controllers;

/// <summary>
/// Khu tham quan du lịch cho khách (US-01).
/// </summary>
[Route("areas")]
[Route("api/v1/areas")]
public class AreasController : BaseApiController
{
    private readonly IAreaService _areaService;
    private readonly IPassService _passService;

    public AreasController(IAreaService areaService, IPassService passService)
    {
        _areaService = areaService;
        _passService = passService;
    }

    /// <summary>
    /// Lấy danh sách các khu tham quan đang hoạt động, có thể lọc theo khoảng cách GPS (US-01).
    /// </summary>
    /// <param name="lat">Vĩ độ của du khách (tùy chọn)</param>
    /// <param name="lng">Kinh độ của du khách (tùy chọn)</param>
    /// <param name="radiusKm">Bán kính tìm kiếm (km, tùy chọn)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AreaSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AreaSummaryDto>>> GetAll(
        [FromQuery] double? lat = null,
        [FromQuery] double? lng = null,
        [FromQuery] double? radiusKm = null,
        CancellationToken cancellationToken = default)
    {
        var areas = await _areaService.GetActiveAreasAsync(lat, lng, radiusKm, cancellationToken);
        return Ok(areas);
    }

    /// <summary>
    /// Lấy thông tin chi tiết khu tham quan và danh sách ngôn ngữ được hỗ trợ từ mã khu (US-01).
    /// </summary>
    /// <param name="code">Mã khu tham quan (ví dụ: van-mieu)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Thông tin chi tiết khu tham quan và các ngôn ngữ hỗ trợ</returns>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(AreaDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AreaDetailResponse>> GetByCode(
        string code,
        CancellationToken cancellationToken)
    {
        var area = await _areaService.GetAreaByCodeAsync(code, cancellationToken);
        return Ok(area);
    }

    /// <summary>
    /// Lấy danh sách điểm di tích (POIs) của một khu tham quan (để hiển thị trên bản đồ và lộ trình).
    /// </summary>
    /// <param name="code">Mã khu tham quan</param>
    /// <param name="lang">Ngôn ngữ hiển thị (mặc định vi)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{code}/pois")]
    [ProducesResponseType(typeof(IReadOnlyList<AreaPoiSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AreaPoiSummaryDto>>> GetPois(
        string code,
        [FromQuery] string? lang = "vi",
        CancellationToken cancellationToken = default)
    {
        var pois = await _areaService.GetAreaPoisAsync(code, lang, cancellationToken);
        return Ok(pois);
    }

    /// <summary>
    /// Mua vé AccessPass vào khu tham quan kèm thanh toán idempotent (US-02).
    /// </summary>
    /// <param name="code">Mã khu tham quan (ví dụ: van-mieu)</param>
    /// <param name="idempotencyKey">Header Idempotency-Key chống trùng lặp thanh toán</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AccessPass mới kèm AccessToken plaintext duy nhất</returns>
    [HttpPost("{code}/passes")]
    [EnableRateLimiting(RateLimitingExtensions.PassCreationPolicy)]
    [ProducesResponseType(typeof(CreatePassResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CreatePassResponse>> CreatePass(
        string code,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ValidationException(
                ErrorCodes.ValidationError,
                "Header 'Idempotency-Key' là bắt buộc để thực hiện giao dịch thanh toán.");
        }

        string? activePassToken = null;
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authStr = authHeader.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(authStr))
            {
                activePassToken = authStr;
            }
        }
        else if (Request.Headers.TryGetValue("X-Prior-Pass-Token", out var priorHeader))
        {
            activePassToken = priorHeader.ToString().Trim();
        }

        var request = new CreatePassRequest
        {
            AreaCode = code,
            IdempotencyKey = idempotencyKey,
            ActivePassToken = activePassToken
        };

        var response = await _passService.CreatePassAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
