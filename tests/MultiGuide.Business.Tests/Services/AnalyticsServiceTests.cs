using Moq;
using MultiGuide.Business.Common;
using MultiGuide.Business.DTOs.Admin;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;
using Xunit;

namespace MultiGuide.Business.Tests.Services;

public class AnalyticsServiceTests
{
    private readonly Mock<IAnalyticsRepository> _repoMock = new();
    private readonly Mock<IClock> _clockMock = new();

    [Fact]
    public async Task GetDashboardStatsAsync_DelegatesToRepository()
    {
        var expected = new DashboardAnalyticsResponse
        {
            TotalAreas = 2,
            TotalPois = 10,
            TotalPasses = 25,
            TotalRevenueUsd = 50m
        };
        _repoMock.Setup(r => r.GetDashboardAnalyticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new AnalyticsService(_repoMock.Object, _clockMock.Object);
        var actual = await service.GetDashboardStatsAsync();

        Assert.Equal(expected, actual);
        _repoMock.Verify(r => r.GetDashboardAnalyticsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHourlyVisitorsAsync_DefaultHours_UsesClockUtcNowAnd12Hours()
    {
        var fixedUtc = new DateTime(2026, 11, 19, 8, 0, 0, DateTimeKind.Utc);
        _clockMock.Setup(c => c.UtcNow).Returns(fixedUtc);

        var expected = new HourlyVisitorsResponse
        {
            Hours = 12,
            GeneratedAtUtc = fixedUtc,
            TotalVisitors = 3
        };
        _repoMock.Setup(r => r.GetHourlyVisitorsAsync(12, fixedUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var service = new AnalyticsService(_repoMock.Object, _clockMock.Object);
        var actual = await service.GetHourlyVisitorsAsync();

        Assert.Equal(12, actual.Hours);
        Assert.Equal(3, actual.TotalVisitors);
        _repoMock.Verify(r => r.GetHourlyVisitorsAsync(12, fixedUtc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(24)]
    public async Task GetHourlyVisitorsAsync_ValidHours_CallsRepository(int hours)
    {
        var fixedUtc = new DateTime(2026, 11, 19, 8, 0, 0, DateTimeKind.Utc);
        _clockMock.Setup(c => c.UtcNow).Returns(fixedUtc);
        _repoMock.Setup(r => r.GetHourlyVisitorsAsync(hours, fixedUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HourlyVisitorsResponse { Hours = hours, GeneratedAtUtc = fixedUtc });

        var service = new AnalyticsService(_repoMock.Object, _clockMock.Object);
        var actual = await service.GetHourlyVisitorsAsync(hours);

        Assert.Equal(hours, actual.Hours);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(100)]
    public async Task GetHourlyVisitorsAsync_OutOfRangeHours_ThrowsValidationException(int hours)
    {
        var service = new AnalyticsService(_repoMock.Object, _clockMock.Object);

        var ex = await Assert.ThrowsAsync<ValidationException>(() => service.GetHourlyVisitorsAsync(hours));
        Assert.Equal(ErrorCodes.ValidationError, ex.Code);
        _repoMock.Verify(
            r => r.GetHourlyVisitorsAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
