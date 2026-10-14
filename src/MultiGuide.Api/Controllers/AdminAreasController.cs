using Microsoft.AspNetCore.Mvc;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Services;

namespace MultiGuide.Api.Controllers;

/// <summary>
/// Quản lý khu tham quan du lịch (TourArea) dành cho Admin (US-10).
/// </summary>
[Route("admin/areas")]
[Route("api/v1/admin/areas")]
public class AdminAreasController : AdminBaseController
{
    private readonly IAdminContentService _adminContentService;

    public AdminAreasController(IAdminContentService adminContentService)
    {
        _adminContentService = adminContentService;
    }

    /// <summary>
    /// Lấy danh sách tất cả các khu tham quan.
    /// </summary>
    /// <param name="isActive">Lọc theo trạng thái hoạt động (null: lấy tất cả)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AreaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AreaResponse>>> GetAll(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var areas = await _adminContentService.GetAreasAsync(isActive, cancellationToken);
        return Ok(areas);
    }

    /// <summary>
    /// Lấy thông tin chi tiết một khu theo ID.
    /// </summary>
    /// <param name="id">Mã định danh khu (UUID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AreaResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var area = await _adminContentService.GetAreaByIdAsync(id, cancellationToken);
        return Ok(area);
    }

    /// <summary>
    /// Tạo mới một khu tham quan du lịch.
    /// </summary>
    /// <param name="request">Thông tin khu cần tạo</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AreaResponse>> Create(
        [FromBody] CreateAreaRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _adminContentService.CreateAreaAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Cập nhật thông tin khu tham quan du lịch.
    /// </summary>
    /// <param name="id">Mã định danh khu</param>
    /// <param name="request">Dữ liệu cập nhật</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AreaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<AreaResponse>> Update(
        Guid id,
        [FromBody] UpdateAreaRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _adminContentService.UpdateAreaAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Xóa mềm một khu tham quan (is_active = false).
    /// </summary>
    /// <param name="id">Mã định danh khu</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _adminContentService.DeleteAreaAsync(id, cancellationToken);
        return NoContent();
    }
}
