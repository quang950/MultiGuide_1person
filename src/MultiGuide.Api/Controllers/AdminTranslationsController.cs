using Microsoft.AspNetCore.Mvc;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Api.Controllers;

/// <summary>
/// Quản lý bản dịch đa ngôn ngữ của POI dành cho Admin (US-12, US-13).
/// </summary>
[Route("admin/pois/{poiId:guid}/translations")]
[Route("api/v1/admin/pois/{poiId:guid}/translations")]
public class AdminTranslationsController : AdminBaseController
{
    private readonly IAdminContentService _adminContentService;

    public AdminTranslationsController(IAdminContentService adminContentService)
    {
        _adminContentService = adminContentService;
    }

    /// <summary>
    /// Lấy danh sách bản dịch của một POI.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TranslationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TranslationResponse>>> GetAll(
        Guid poiId,
        CancellationToken cancellationToken = default)
    {
        var translations = await _adminContentService.GetTranslationsAsync(poiId, cancellationToken);
        return Ok(translations);
    }

    /// <summary>
    /// Lấy chi tiết một bản dịch.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TranslationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TranslationResponse>> GetById(
        Guid poiId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var translation = await _adminContentService.GetTranslationByIdAsync(poiId, id, cancellationToken);
        return Ok(translation);
    }

    /// <summary>
    /// Tạo bản dịch cho POI (mỗi cặp POI + ngôn ngữ chỉ có một bản dịch).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TranslationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TranslationResponse>> Create(
        Guid poiId,
        [FromBody] CreateTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        var created = await _adminContentService.CreateTranslationAsync(poiId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { poiId, id = created.Id }, created);
    }

    /// <summary>
    /// Cập nhật tiêu đề và mô tả của bản dịch.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TranslationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TranslationResponse>> Update(
        Guid poiId,
        Guid id,
        [FromBody] UpdateTranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        var updated = await _adminContentService.UpdateTranslationAsync(poiId, id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Xóa bản dịch.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid poiId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _adminContentService.DeleteTranslationAsync(poiId, id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Tải lên file audio MP3 cho bản dịch POI (US-13).
    /// </summary>
    [HttpPost("/admin/translations/{id:guid}/audio")]
    [HttpPost("/api/v1/admin/translations/{id:guid}/audio")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(TranslationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<TranslationResponse>> UploadAudio(
        Guid id,
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Vui lòng chọn file audio MP3.");
        }

        await using var stream = file.OpenReadStream();
        var result = await _adminContentService.UploadTranslationAudioAsync(
            id,
            stream,
            file.Length,
            file.ContentType,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Sinh file audio MP3 tự động bằng Edge Neural TTS từ kịch bản văn bản (AI Voice).
    /// </summary>
    [HttpPost("/admin/translations/{id:guid}/generate-audio")]
    [HttpPost("/api/v1/admin/translations/{id:guid}/generate-audio")]
    [ProducesResponseType(typeof(GenerateAudioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<GenerateAudioResponse>> GenerateAudio(
        Guid id,
        [FromBody] GenerateAudioRequest? request,
        CancellationToken cancellationToken = default)
    {
        var result = await _adminContentService.GenerateTranslationAudioAsync(
            id,
            request?.Text,
            cancellationToken);

        return Ok(result);
    }
}

