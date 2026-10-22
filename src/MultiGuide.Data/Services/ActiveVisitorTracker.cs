using System.Collections.Concurrent;
using MultiGuide.Business.Common;
using MultiGuide.Business.Services;

namespace MultiGuide.Data.Services;

/// <summary>
/// Quản lý và tính toán số lượng du khách trực tuyến bằng cơ chế In-Memory Sliding Window.
/// </summary>
public class ActiveVisitorTracker : IVisitorTracker
{
    private readonly ConcurrentDictionary<Guid, DateTime> _activePings = new();
    private readonly IClock? _clock;
    private static readonly TimeSpan DefaultWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PurgeThreshold = TimeSpan.FromMinutes(5);

    public ActiveVisitorTracker(IClock? clock = null)
    {
        _clock = clock;
    }

    private DateTime CurrentUtc => _clock?.UtcNow ?? DateTime.UtcNow;

    public void RecordHeartbeat(Guid passId)
    {
        if (passId == Guid.Empty) return;

        var now = CurrentUtc;
        _activePings[passId] = now;

        // Tự động dọn dẹp nhẹ nhàng nếu danh mục lưu trữ lớn
        if (_activePings.Count > 100)
        {
            PurgeStaleEntries(now);
        }
    }

    public int GetActiveVisitorCount(TimeSpan? window = null)
    {
        var now = CurrentUtc;
        var effectiveWindow = window ?? DefaultWindow;
        var cutoffTime = now - effectiveWindow;

        var activeCount = 0;
        foreach (var kvp in _activePings)
        {
            if (kvp.Value >= cutoffTime)
            {
                activeCount++;
            }
        }

        // Tự động dọn các entry đã quá cũ (> 5 phút)
        PurgeStaleEntries(now);

        return activeCount;
    }

    private void PurgeStaleEntries(DateTime now)
    {
        var staleCutoff = now - PurgeThreshold;
        foreach (var kvp in _activePings)
        {
            if (kvp.Value < staleCutoff)
            {
                _activePings.TryRemove(kvp.Key, out _);
            }
        }
    }
}
