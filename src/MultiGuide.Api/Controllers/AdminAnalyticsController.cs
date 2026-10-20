using Microsoft.AspNetCore.Mvc;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Services;

namespace MultiGuide.Api.Controllers;

/// <summary>
/// Thống kê dữ liệu hành vi du khách và hiệu suất nội dung dành cho Admin.
/// </summary>
[Route("admin/analytics")]
[Route("api/v1/admin/analytics")]
public class AdminAnalyticsController : AdminBaseController
{
    private readonly IAnalyticsService _analyticsService;

    public AdminAnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Lấy báo cáo thống kê tổng quan (Dashboard Analytics): Top POI yêu thích, ngôn ngữ, vé, doanh thu.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardAnalyticsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DashboardAnalyticsResponse>> GetDashboard(CancellationToken cancellationToken = default)
    {
        var stats = await _analyticsService.GetDashboardStatsAsync(cancellationToken);
        return Ok(stats);
    }
}
