using MultiGuide.Business.Common;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Repositories;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Services;

public class PlayLogService : IPlayLogService
{
    private readonly IPlayLogRepository _playLogRepository;
    private readonly IPoiRepository _poiRepository;
    private readonly IClock _clock;

    public PlayLogService(
        IPlayLogRepository playLogRepository,
        IPoiRepository poiRepository,
        IClock clock)
    {
        _playLogRepository = playLogRepository;
        _poiRepository = poiRepository;
        _clock = clock;
    }

    public async Task<PlayLogResponse> CreatePlayLogAsync(
        Guid passId,
        Guid passAreaId,
        CreatePlayLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.PoiId == Guid.Empty)
        {
            errors["poi_id"] = ["Id của POI là bắt buộc."];
        }

        if (string.IsNullOrWhiteSpace(request.LanguageCode))
        {
            errors["language_code"] = ["Mã ngôn ngữ là bắt buộc."];
        }
        else if (request.LanguageCode.Trim().Length < 2 || request.LanguageCode.Trim().Length > 10)
        {
            errors["language_code"] = ["Mã ngôn ngữ phải từ 2 đến 10 ký tự."];
        }

        if (request.ListenedSec < 0 || request.ListenedSec > 21600)
        {
            errors["listened_sec"] = ["Thời lượng nghe phải nằm trong khoảng từ 0 đến 21600 giây (6 giờ)."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(ErrorCodes.ValidationError, "Dữ liệu nhật ký nghe không hợp lệ.", errors);
        }

        var poi = await _poiRepository.GetByIdAsync(request.PoiId, cancellationToken);
        if (poi == null || !poi.IsActive)
        {
            throw new NotFoundException(
                ErrorCodes.PoiContentNotFound,
                $"Điểm tham quan '{request.PoiId}' không tồn tại hoặc đã ngừng hoạt động.");
        }

        if (poi.AreaId != passAreaId)
        {
            throw new ForbiddenException(
                ErrorCodes.PassAreaMismatch,
                "AccessPass không thuộc khu vực của điểm tham quan này.");
        }

        var playLog = new PlayLog
        {
            Id = Guid.NewGuid(),
            PassId = passId,
            PoiId = request.PoiId,
            LanguageCode = request.LanguageCode.Trim().ToLowerInvariant(),
            StartedAt = _clock.UtcNow,
            ListenedSec = request.ListenedSec,
            Completed = request.Completed
        };

        await _playLogRepository.AddAsync(playLog, cancellationToken);

        return new PlayLogResponse
        {
            Id = playLog.Id,
            PassId = playLog.PassId,
            PoiId = playLog.PoiId,
            LanguageCode = playLog.LanguageCode,
            StartedAt = playLog.StartedAt,
            ListenedSec = playLog.ListenedSec,
            Completed = playLog.Completed
        };
    }
}
