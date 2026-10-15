using MultiGuide.Business.Common;

namespace MultiGuide.Business.Tests.Common;

public class SystemClockTests
{
    [Fact]
    public void UtcNow_ReturnsCurrentUtcTimeWithinTolerance()
    {
        // Arrange
        IClock clock = new SystemClock();
        var before = DateTime.UtcNow;

        // Act
        var result = clock.UtcNow;

        // Assert
        var after = DateTime.UtcNow;
        Assert.True(result >= before && result <= after,
            $"Expected UtcNow {result:O} to be between {before:O} and {after:O}");
        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }
}
