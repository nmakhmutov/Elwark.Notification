using Notification.Api.Scheduling;

namespace Notification.Api.Tests.Scheduling;

public sealed class EmailScheduleCalculatorTests
{
    // ── No timezone ───────────────────────────────────────────────────────────

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

    [Fact]
    public void CalculateSendAt_WithNullTimezone_ShouldReturnExactSameValue()
    {
        var now = new DateTime(2024, 6, 1, 3, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, null);

        Assert.Equal(now, result);
    }

    // ── Business hours boundaries (UTC) ───────────────────────────────────────

    [Theory]
    [InlineData(9)]   // start of business hours (inclusive)
    [InlineData(12)]  // midday
    [InlineData(20)]  // last full hour inside window
    public void CalculateSendAt_DuringBusinessHours_ShouldReturnNow(int hour)
    {
        var now = new DateTime(2024, 3, 15, hour, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "UTC");

        Assert.Equal(now, result);
    }

    [Fact]
    public void CalculateSendAt_At20_59_59_ShouldReturnNow()
    {
        // Last second still inside business hours
        var now = new DateTime(2024, 3, 15, 20, 59, 59, DateTimeKind.Utc);

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

    [Fact]
    public void CalculateSendAt_At8_59_59_ShouldScheduleFor9amSameDay()
    {
        // One second before business hours open
        var now = new DateTime(2024, 3, 15, 8, 59, 59, DateTimeKind.Utc);
        var expected = new DateTime(2024, 3, 15, 9, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "UTC");

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(21)]  // first hour after window (not < 21)
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
    public void CalculateSendAt_AtMidnight_ShouldScheduleFor9amSameDay()
    {
        var now = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(2024, 3, 15, 9, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "UTC");

        Assert.Equal(expected, result);
    }

    // ── Scheduled time precision ──────────────────────────────────────────────

    [Fact]
    public void CalculateSendAt_ScheduledTime_ShouldHaveZeroSecondsAndMinutes()
    {
        var now = new DateTime(2024, 3, 15, 7, 30, 45, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "UTC");

        Assert.Equal(9, result.Hour);
        Assert.Equal(0, result.Minute);
        Assert.Equal(0, result.Second);
    }

    // ── Invalid timezone ──────────────────────────────────────────────────────

    [Fact]
    public void CalculateSendAt_WithInvalidTimezone_ShouldFallbackToUtc()
    {
        var now = new DateTime(2024, 3, 15, 7, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(2024, 3, 15, 9, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "Not/A/Timezone");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateSendAt_WithInvalidTimezone_DuringBusinessHours_ShouldReturnNow()
    {
        // Falls back to UTC; 14:00 UTC is business hours
        var now = new DateTime(2024, 3, 15, 14, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "Bogus/Zone");

        Assert.Equal(now, result);
    }

    // ── Non-UTC timezone (Asia/Tokyo = UTC+9, no DST) ─────────────────────────

    [Fact]
    public void CalculateSendAt_TokyoTimezone_WhenLocalTimeIs9am_ShouldReturnNow()
    {
        // UTC 00:00 = Tokyo 09:00 → business hours → send now
        var now = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "Asia/Tokyo");

        Assert.Equal(now, result);
    }

    [Fact]
    public void CalculateSendAt_TokyoTimezone_WhenLocalTimeIsAfterHours_ShouldScheduleForNextDayTokyo()
    {
        // UTC 12:00 = Tokyo 21:00 → after hours → 9am next day Tokyo = UTC 00:00 next day
        var now = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(2024, 1, 16, 0, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "Asia/Tokyo");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateSendAt_TokyoTimezone_WhenLocalTimeIsBeforeHours_ShouldScheduleFor9amSameDayTokyo()
    {
        // UTC 23:00 = Tokyo 08:00 next day → before 9am → 9am same Tokyo day = UTC 00:00 next day
        var now = new DateTime(2024, 1, 15, 23, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(2024, 1, 16, 0, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "Asia/Tokyo");

        Assert.Equal(expected, result);
    }

    // ── Month / year rollover ─────────────────────────────────────────────────

    [Fact]
    public void CalculateSendAt_AfterHours_OnLastDayOfMonth_ShouldRollToFirstDayOfNextMonth()
    {
        var now = new DateTime(2024, 3, 31, 22, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(2024, 4, 1, 9, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "UTC");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateSendAt_AfterHours_OnNewYearsEve_ShouldRollToNewYear()
    {
        var now = new DateTime(2024, 12, 31, 22, 0, 0, DateTimeKind.Utc);
        var expected = new DateTime(2025, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        var result = EmailScheduleCalculator.CalculateSendAt(now, "UTC");

        Assert.Equal(expected, result);
    }
}
