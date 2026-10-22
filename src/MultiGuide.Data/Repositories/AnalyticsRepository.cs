using Microsoft.EntityFrameworkCore;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Data.Context;

namespace MultiGuide.Data.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly MultiGuideDbContext _context;
    private readonly IVisitorTracker? _visitorTracker;

    public AnalyticsRepository(MultiGuideDbContext context, IVisitorTracker? visitorTracker = null)
    {
        _context = context;
        _visitorTracker = visitorTracker;
    }

    public async Task<DashboardAnalyticsResponse> GetDashboardAnalyticsAsync(CancellationToken cancellationToken = default)
    {
        var totalAreas = await _context.TourAreas.CountAsync(a => a.IsActive, cancellationToken);
        var totalPois = await _context.Pois.CountAsync(p => p.IsActive, cancellationToken);
        var totalPasses = await _context.AccessPasses.CountAsync(cancellationToken);

        var totalRevenueUsd = await _context.Payments
            .Where(p => p.Status == MultiGuide.Domain.Enums.PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var totalPlayLogs = await _context.PlayLogs.CountAsync(cancellationToken);
        var totalListenedSec = await _context.PlayLogs.SumAsync(p => (int?)p.ListenedSec, cancellationToken) ?? 0;

        // Top 10 POIs được nghe nhiều nhất
        var topPoiGroups = await _context.PlayLogs
            .GroupBy(p => p.PoiId)
            .Select(g => new
            {
                PoiId = g.Key,
                PlayCount = g.Count(),
                TotalListenedSec = g.Sum(x => x.ListenedSec),
                CompletedCount = g.Count(x => x.Completed)
            })
            .OrderByDescending(x => x.PlayCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        var poiIds = topPoiGroups.Select(x => x.PoiId).ToList();
        var pois = await _context.Pois
            .Include(p => p.Area)
            .Include(p => p.Translations)
            .Where(p => poiIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var topPois = topPoiGroups.Select(g =>
        {
            var poi = pois.GetValueOrDefault(g.PoiId);
            var title = poi?.Translations.FirstOrDefault(t => t.LanguageCode == "vi")?.Title
                     ?? poi?.Translations.FirstOrDefault()?.Title
                     ?? "Không có tiêu đề";
            var areaName = poi?.Area?.Name ?? "Không xác định";

            return new PoiStatDto(
                g.PoiId,
                title,
                areaName,
                g.PlayCount,
                g.TotalListenedSec,
                g.CompletedCount
            );
        }).ToList();

        // Thống kê phân bổ theo ngôn ngữ
        var langGroups = await _context.PlayLogs
            .GroupBy(p => p.LanguageCode)
            .Select(g => new
            {
                LanguageCode = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var languages = await _context.Languages.ToDictionaryAsync(l => l.Code, l => l.Name, cancellationToken);

        var languageDistribution = langGroups.Select(g => new LanguageStatDto(
            g.LanguageCode,
            languages.GetValueOrDefault(g.LanguageCode, g.LanguageCode.ToUpperInvariant()),
            g.Count,
            totalPlayLogs > 0 ? Math.Round((double)g.Count / totalPlayLogs * 100, 1) : 0
        ))
        .OrderByDescending(x => x.PlayCount)
        .ToList();

        var activeVisitors = _visitorTracker?.GetActiveVisitorCount(TimeSpan.FromSeconds(30)) ?? 0;

        return new DashboardAnalyticsResponse
        {
            TotalAreas = totalAreas,
            TotalPois = totalPois,
            TotalPasses = totalPasses,
            TotalRevenueUsd = totalRevenueUsd,
            TotalPlayLogs = totalPlayLogs,
            TotalListenedSec = totalListenedSec,
            ActiveVisitorsCount = activeVisitors,
            TopPois = topPois,
            LanguageDistribution = languageDistribution
        };
    }
}
