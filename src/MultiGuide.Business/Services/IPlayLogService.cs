using MultiGuide.Business.DTOs.Client;

namespace MultiGuide.Business.Services;

/// <summary>
/// Ghi nhận nhật ký nghe thuyết minh âm thanh của khách tham quan (US-08).
/// </summary>
public interface IPlayLogService
{
    Task<PlayLogResponse> CreatePlayLogAsync(
        Guid passId,
        Guid passAreaId,
        CreatePlayLogRequest request,
        CancellationToken cancellationToken = default);
}
