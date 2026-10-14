using Microsoft.AspNetCore.Mvc;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Services;

namespace MultiGuide.Api.Controllers;

/// <summary>
/// Khu tham quan du lịch cho khách (US-01).
/// </summary>
[Route("areas")]
[Route("api/v1/areas")]
public class AreasController : BaseApiController
{
    private readonly IAreaService _areaService;

    public AreasController(IAreaService areaService)
    {
        _areaService = areaService;
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
}
