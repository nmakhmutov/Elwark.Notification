using Notification.Api.Extensions;

namespace Notification.Api.Tests.Extensions;

public sealed class DateTimeExtensionsTests
{
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
    public void TruncateToMinute_ShouldPreserveKind()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Local);

        var result = dt.TruncateToMinute();

        Assert.Equal(DateTimeKind.Local, result.Kind);
    }

    [Fact]
    public void CeilingToMinute_WhenOnBoundary_ShouldReturnSame()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 0, 0, DateTimeKind.Utc);

        var result = dt.CeilingToMinute();

        Assert.Equal(dt, result);
    }

    [Fact]
    public void CeilingToMinute_WhenNotOnBoundary_ShouldRoundUpToNextMinute()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 45, 500, DateTimeKind.Utc);

        var result = dt.CeilingToMinute();

        Assert.Equal(new DateTime(2024, 3, 15, 10, 31, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void CeilingToMinute_WithOneTick_ShouldRoundUpToNextMinute()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 0, 0, DateTimeKind.Utc).AddTicks(1);

        var result = dt.CeilingToMinute();

        Assert.Equal(new DateTime(2024, 3, 15, 10, 31, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void CeilingToMinute_ShouldPreserveKind()
    {
        var dt = new DateTime(2024, 3, 15, 10, 30, 45, DateTimeKind.Local);

        var result = dt.CeilingToMinute();

        Assert.Equal(DateTimeKind.Local, result.Kind);
    }
}
