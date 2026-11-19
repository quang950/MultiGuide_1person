using MultiGuide.Business.DTOs.Admin;

namespace MultiGuide.Business.Services;

public interface IAnalyticsService
{
    Task<DashboardAnalyticsResponse> GetDashboardStatsAsync(CancellationToken cancellationToken = default);
    Task<HourlyVisitorsResponse> GetHourlyVisitorsAsync(int hours = 12, CancellationToken cancellationToken = default);
}
