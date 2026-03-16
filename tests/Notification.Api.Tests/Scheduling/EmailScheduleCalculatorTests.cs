using Notification.Api.Scheduling;

namespace Notification.Api.Tests.Scheduling;

public sealed class EmailScheduleCalculatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CalculateSendAt_WithNullOrEmptyTimezone_ShouldReturnNow(string? timezone)
    {
        var now = new DateTime(2024, 3, 15, 14, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, timezone);

        Assert.Equal(now, result);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(12)]
    [InlineData(20)]
    public void CalculateSendAt_DuringBusinessHours_ShouldReturnNow(int hour)
    {
        var now = new DateTime(2024, 3, 15, hour, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "UTC");

        Assert.Equal(now, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(8)]
    public void CalculateSendAt_BeforeBusinessHours_ShouldScheduleFor9amSameDay(int hour)
    {
        var now = new DateTime(2024, 3, 15, hour, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(2024, 3, 15, 9, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "UTC");

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(21)]
    [InlineData(22)]
    [InlineData(23)]
    public void CalculateSendAt_AfterBusinessHours_ShouldScheduleFor9amNextDay(int hour)
    {
        var now = new DateTime(2024, 3, 15, hour, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(2024, 3, 16, 9, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "UTC");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateSendAt_WithInvalidTimezone_ShouldFallbackToUtc()
    {
        // Invalid timezone → defaults to UTC; 07:00 UTC is before 9am
        var now = new DateTime(2024, 3, 15, 7, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(2024, 3, 15, 9, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "Not/A/Timezone");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateSendAt_ShouldScheduleFor_ExactlyMidnight9am()
    {
        var now = new DateTime(2024, 3, 15, 7, 30, 45, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "UTC");

        Assert.Equal(0, result.Second);
        Assert.Equal(0, result.Minute);
        Assert.Equal(9, result.Hour);
    }
}
