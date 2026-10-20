using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Repositories;

namespace MultiGuide.Business.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;

    public AnalyticsService(IAnalyticsRepository analyticsRepository)
    {
        _analyticsRepository = analyticsRepository;
    }

    public Task<DashboardAnalyticsResponse> GetDashboardStatsAsync(CancellationToken cancellationToken = default)
    {
        return _analyticsRepository.GetDashboardAnalyticsAsync(cancellationToken);
    }
}
