using MultiGuide.Business.Common;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Repositories;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Business.Services;

public class AnalyticsService : IAnalyticsService
{
    private const int MinHours = 1;
    private const int MaxHours = 24;

    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IClock? _clock;

    public AnalyticsService(IAnalyticsRepository analyticsRepository, IClock? clock = null)
    {
        _analyticsRepository = analyticsRepository;
        _clock = clock;
    }

    public Task<DashboardAnalyticsResponse> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        return _analyticsRepository.GetDashboardAnalyticsAsync(cancellationToken);
    }

    public Task<HourlyVisitorsResponse> GetHourlyVisitorsAsync(int hours = 12, CancellationToken cancellationToken = default)
    {
        if (hours < MinHours || hours > MaxHours)
        {
            var errors = new Dictionary<string, string[]>
            {
                ["hours"] = [$"Tham số 'hours' phải nằm trong khoảng từ {MinHours} đến {MaxHours}."]
            };
            throw new ValidationException(ErrorCodes.ValidationError, "Số giờ thống kê không hợp lệ.", errors);
        }

        var utcNow = _clock?.UtcNow ?? DateTime.UtcNow;
        return _analyticsRepository.GetHourlyVisitorsAsync(hours, utcNow, cancellationToken);
    }
}
