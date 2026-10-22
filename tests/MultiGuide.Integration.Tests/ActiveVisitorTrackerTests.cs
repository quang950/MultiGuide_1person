using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Business.Common;
using MultiGuide.Business.Services;
using MultiGuide.Data.Context;
using MultiGuide.Data.Services;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Enums;
using Xunit;

namespace MultiGuide.Integration.Tests;

public class ActiveVisitorTrackerTests
{
    private class FakeClock : IClock
    {
        public DateTime UtcNow { get; set; } = DateTime.UtcNow;
    }

    [Fact]
    public void RecordHeartbeat_TangSoLuongKhachHoatDong()
    {
        var clock = new FakeClock();
        var tracker = new ActiveVisitorTracker(clock);
        var passId = Guid.NewGuid();

        tracker.RecordHeartbeat(passId);

        var count = tracker.GetActiveVisitorCount(TimeSpan.FromSeconds(30));
        Assert.Equal(1, count);
    }

    [Fact]
    public void RecordHeartbeat_EmptyGuid_KhongGhiNhan()
    {
        var tracker = new ActiveVisitorTracker();
        tracker.RecordHeartbeat(Guid.Empty);

        var count = tracker.GetActiveVisitorCount();
        Assert.Equal(0, count);
    }

    [Fact]
    public void RecordHeartbeat_CungMotKhachPingNhieuLan_KhongBiDemTrung()
    {
        var clock = new FakeClock();
        var tracker = new ActiveVisitorTracker(clock);
        var passId = Guid.NewGuid();

        tracker.RecordHeartbeat(passId);
        tracker.RecordHeartbeat(passId);
        tracker.RecordHeartbeat(passId);

        var count = tracker.GetActiveVisitorCount();
        Assert.Equal(1, count);
    }

    [Fact]
    public void GetActiveVisitorCount_QuaThoiGianWindow_TuDongTruKhach()
    {
        var clock = new FakeClock { UtcNow = new DateTime(2026, 10, 22, 10, 0, 0, DateTimeKind.Utc) };
        var tracker = new ActiveVisitorTracker(clock);
        var passId = Guid.NewGuid();

        tracker.RecordHeartbeat(passId);
        Assert.Equal(1, tracker.GetActiveVisitorCount(TimeSpan.FromSeconds(30)));

        // Trôi qua 35 giây (quá ngưỡng 30s)
        clock.UtcNow = clock.UtcNow.AddSeconds(35);

        Assert.Equal(0, tracker.GetActiveVisitorCount(TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void GetActiveVisitorCount_NhieuKhach_DemChinhXac()
    {
        var clock = new FakeClock { UtcNow = new DateTime(2026, 10, 22, 10, 0, 0, DateTimeKind.Utc) };
        var tracker = new ActiveVisitorTracker(clock);

        var guest1 = Guid.NewGuid();
        var guest2 = Guid.NewGuid();
        var guest3 = Guid.NewGuid();

        tracker.RecordHeartbeat(guest1);
        tracker.RecordHeartbeat(guest2);

        // Khách 3 vào sau 10 giây
        clock.UtcNow = clock.UtcNow.AddSeconds(10);
        tracker.RecordHeartbeat(guest3);

        // Lúc này cả 3 đều trong vòng 30s
        Assert.Equal(3, tracker.GetActiveVisitorCount(TimeSpan.FromSeconds(30)));

        // Trôi thêm 25 giây (tổng cộng 35s so với khách 1 và 2)
        clock.UtcNow = clock.UtcNow.AddSeconds(25);

        // Khách 1 và 2 đã quá 30s, chỉ còn khách 3 còn hạn
        Assert.Equal(1, tracker.GetActiveVisitorCount(TimeSpan.FromSeconds(30)));
    }
}
