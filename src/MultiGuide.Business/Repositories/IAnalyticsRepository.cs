using MultiGuide.Business.DTOs.Admin;

namespace MultiGuide.Business.Repositories;

public interface IAnalyticsRepository
{
    Task<DashboardAnalyticsResponse> GetDashboardAnalyticsAsync(CancellationToken cancellationToken = default);
}
