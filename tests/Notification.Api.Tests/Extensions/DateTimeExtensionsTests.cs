using Notification.Api.Extensions;

namespace Notification.Api.Tests.Extensions;

public sealed class DateTimeExtensionsTests
{
    [Fact]
    public void CeilingToMinute_AtEndOfDay_ShouldRollToNextDayMidnight()
    {
        var dt = new DateTime(2024, 3, 15, 23, 59, 30, 0, DateTimeKind.Utc);

        var result = dt.CeilingToMinute();

        Assert.Equal(new DateTime(2024, 3, 16, 0, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void CeilingToMinute_AtLastMinuteOnBoundary_ShouldNotRollOver()
    {
        var dt = new DateTime(2024, 3, 15, 23, 59, 0, 0, DateTimeKind.Utc);

        var result = dt.CeilingToMinute();

        Assert.Equal(dt, result);
    }

    [Fact]
    public void CeilingToMinute_AtMidnight_ShouldReturnMidnight()
    {
        var dt = new DateTime(2024, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc);

        var result = dt.CeilingToMinute();

        Assert.Equal(dt, result);
    }

    [Fact]
    public void CeilingToMinute_ShouldPreserveLocalKind()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Local);

        var result = dt.CeilingToMinute();

        Assert.Equal(DateTimeKind.Local, result.Kind);
    }

    [Fact]
    public void CeilingToMinute_ShouldPreserveUnspecifiedKind()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Unspecified);

        var result = dt.CeilingToMinute();

        Assert.Equal(DateTimeKind.Unspecified, result.Kind);
    }

    [Fact]
    public void CeilingToMinute_ShouldPreserveUtcKind()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Utc);

        var result = dt.CeilingToMinute();

        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }

    [Fact]
    public void CeilingToMinute_WhenNotOnBoundary_ShouldRoundUpToNextMinute()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 45, 500, DateTimeKind.Utc);

        var result = dt.CeilingToMinute();

        Assert.Equal(new DateTime(2024, 3, 15, 10, 31, 0, 0, DateTimeKind.Utc), result);
    }

    // ── CeilingToMinute ───────────────────────────────────────────────────────

    [Fact]
    public void CeilingToMinute_WhenOnBoundary_ShouldReturnSame()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 0, 0, DateTimeKind.Utc);

        var result = dt.CeilingToMinute();

        Assert.Equal(dt, result);
    }

    [Fact]
    public void CeilingToMinute_WithOneTick_ShouldRoundUpToNextMinute()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 0, 0, DateTimeKind.Utc).AddTicks(1);

        var result = dt.CeilingToMinute();

        Assert.Equal(new DateTime(2024, 3, 15, 10, 31, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void TruncateToMinute_AtEndOfDay_ShouldTruncateToLastMinute()
    {
        var dt = new DateTime(2024, 3, 15, 23, 59, 59, 999, DateTimeKind.Utc);

        var result = dt.TruncateToMinute();

        Assert.Equal(new DateTime(2024, 3, 15, 23, 59, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void TruncateToMinute_AtMidnight_ShouldReturnMidnight()
    {
        var dt = new DateTime(2024, 3, 15, 0, 0, 0, 0, DateTimeKind.Utc);

        var result = dt.TruncateToMinute();

        Assert.Equal(dt, result);
    }

    [Fact]
    public void TruncateToMinute_ShouldPreserveLocalKind()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Local);

        var result = dt.TruncateToMinute();

        Assert.Equal(DateTimeKind.Local, result.Kind);
    }

    [Fact]
    public void TruncateToMinute_ShouldPreserveUnspecifiedKind()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Unspecified);

        var result = dt.TruncateToMinute();

        Assert.Equal(DateTimeKind.Unspecified, result.Kind);
    }

    [Fact]
    public void TruncateToMinute_ShouldPreserveUtcKind()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Utc);

        var result = dt.TruncateToMinute();

        Assert.Equal(DateTimeKind.Utc, result.Kind);
    }
    // ── TruncateToMinute ──────────────────────────────────────────────────────

    [Fact]
    public void TruncateToMinute_ShouldRemoveSecondsAndSubseconds()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 45, 500, DateTimeKind.Utc);

        var result = dt.TruncateToMinute();

        Assert.Equal(new DateTime(2024, 3, 15, 10, 30, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void TruncateToMinute_WhenAlreadyOnBoundary_ShouldReturnSame()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 0, 0, DateTimeKind.Utc);

        var result = dt.TruncateToMinute();

        Assert.Equal(dt, result);
    }

    [Fact]
    public void TruncateToMinute_WithOnlyMilliseconds_ShouldTruncate()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 0, 999, DateTimeKind.Utc);

        var result = dt.TruncateToMinute();

        Assert.Equal(new DateTime(2024, 3, 15, 10, 30, 0, 0, DateTimeKind.Utc), result);
    }
}
