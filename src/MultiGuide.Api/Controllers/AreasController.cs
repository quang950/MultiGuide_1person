using Microsoft.AspNetCore.Mvc;
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
    /// Mua vé AccessPass vào khu tham quan kèm thanh toán idempotent (US-02).
    /// </summary>
    /// <param name="code">Mã khu tham quan (ví dụ: van-mieu)</param>
    /// <param name="idempotencyKey">Header Idempotency-Key chống trùng lặp thanh toán</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AccessPass mới kèm AccessToken plaintext duy nhất</returns>
    [HttpPost("{code}/passes")]
    [ProducesResponseType(typeof(CreatePassResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
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

        var request = new CreatePassRequest
        {
            AreaCode = code,
            IdempotencyKey = idempotencyKey
        };

        var response = await _passService.CreatePassAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
