using Microsoft.AspNetCore.Mvc;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Services;

namespace MultiGuide.Api.Controllers;

/// <summary>
/// Quản lý điểm tham quan (POI) dành cho Admin (US-11).
/// </summary>
[Route("admin/pois")]
[Route("api/v1/admin/pois")]
public class AdminPoisController : AdminBaseController
{
    private readonly IAdminContentService _adminContentService;

    public AdminPoisController(IAdminContentService adminContentService)
    {
        _adminContentService = adminContentService;
    }

    /// <summary>
    /// Lấy danh sách POI có phân trang, lọc theo khu và trạng thái.
    /// </summary>
    /// <param name="areaId">Lọc theo khu (UUID)</param>
    /// <param name="isActive">Lọc theo trạng thái hoạt động</param>
    /// <param name="page">Trang (bắt đầu từ 1)</param>
    /// <param name="pageSize">Số phần tử mỗi trang (1-100, mặc định 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<PoiResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PagedResponse<PoiResponse>>> GetAll(
        [FromQuery] Guid? areaId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery(Name = "page_size")] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminContentService.GetPoisAsync(areaId, isActive, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lấy chi tiết một POI theo ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PoiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PoiResponse>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var poi = await _adminContentService.GetPoiByIdAsync(id, cancellationToken);
        return Ok(poi);
    }

    /// <summary>
    /// Tạo mới một POI thuộc một khu.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PoiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PoiResponse>> Create(
        [FromBody] CreatePoiRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _adminContentService.CreatePoiAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Cập nhật thông tin POI.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PoiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PoiResponse>> Update(
        Guid id,
        [FromBody] UpdatePoiRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _adminContentService.UpdatePoiAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Xóa mềm POI (is_active = false).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromQuery] bool hard = false,
        CancellationToken cancellationToken = default)
    {
        await _adminContentService.DeletePoiAsync(id, hard, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Sinh đoạn văn tóm tắt lịch sử / câu chuyện thuyết minh cho POI bằng Gemini AI (US-19).
    /// </summary>
    /// <param name="id">Mã POI</param>
    /// <param name="request">Yêu cầu (chứa language, mặc định vi)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{id:guid}/ai-summary")]
    [HttpPost("{id:guid}/generate-story")]
    [ProducesResponseType(typeof(AiSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AiSummaryResponse>> GenerateAiSummary(
        Guid id,
        [FromBody] GenerateAiSummaryRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var language = request?.Language ?? "vi";
        var topic = request?.Topic;
        var result = await _adminContentService.GeneratePoiSummaryAsync(id, language, topic, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Tải lên ảnh đại diện / hiện vật cho POI (lưu vào thư mục client/assets/pois).
    /// </summary>
    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadImage(
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { detail = "Vui lòng chọn file hình ảnh." });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif"))
        {
            return BadRequest(new { detail = "Định dạng ảnh không được hỗ trợ. Vui lòng chọn file JPG, PNG, WEBP." });
        }

        var repoRoot = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(repoRoot) && !Directory.Exists(Path.Combine(repoRoot, "client")))
        {
            var parent = Directory.GetParent(repoRoot);
            if (parent == null) break;
            repoRoot = parent.FullName;
        }

        var targetDir = !string.IsNullOrEmpty(repoRoot) && Directory.Exists(Path.Combine(repoRoot, "client"))
            ? Path.Combine(repoRoot, "client", "assets", "pois")
            : Path.Combine(AppContext.BaseDirectory, "assets", "pois");

        Directory.CreateDirectory(targetDir);
        var fileName = $"poi_{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(targetDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        return Ok(new { imageUrl = $"assets/pois/{fileName}" });
    }
}