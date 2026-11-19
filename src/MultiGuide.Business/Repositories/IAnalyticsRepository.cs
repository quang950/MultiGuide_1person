using MultiGuide.Business.DTOs.Admin;

namespace MultiGuide.Business.Repositories;

public interface IAnalyticsRepository
{
    Task<DashboardAnalyticsResponse> GetDashboardAnalyticsAsync(CancellationToken cancellationToken = default);
    Task<HourlyVisitorsResponse> GetHourlyVisitorsAsync(int hours, DateTime utcNow, CancellationToken cancellationToken = default);
}
