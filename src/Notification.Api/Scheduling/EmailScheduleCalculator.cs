namespace Notification.Api.Scheduling;

internal static class EmailScheduleCalculator
{
    public static DateTime CalculateSendAt(DateTime now, string? timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            return now;

        var timezone = ParseTimeZone(timeZone);
        var local = TimeZoneInfo.ConvertTimeFromUtc(now, timezone);

        if (local.Hour is >= 9 and < 21)
            return now;

        var date = local.Hour < 9 ? local : local.AddDays(1);

        return TimeZoneInfo.ConvertTimeToUtc(new DateTime(date.Year, date.Month, date.Day, 9, 0, 0), timezone);
    }

    private static TimeZoneInfo ParseTimeZone(string timeZone)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch
        {
            return TimeZoneInfo.Utc;
        }
    }
}
