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

    /// <summary>
    /// Thống kê lượng khách (AccessPass) vào theo từng khung giờ trong N giờ gần nhất (1 - 24 giờ).
    /// </summary>
    [HttpGet("hourly-visitors")]
    [ProducesResponseType(typeof(HourlyVisitorsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<HourlyVisitorsResponse>> GetHourlyVisitors(
        [FromQuery] int hours = 12,
        CancellationToken cancellationToken = default)
    {
        var result = await _analyticsService.GetHourlyVisitorsAsync(hours, cancellationToken);
        return Ok(result);
    }
}
