namespace Notification.Api.Extensions;

internal static class DateTimeExtensions
{
    extension(DateTime dt)
    {
        public DateTime TruncateToMinute() =>
            new(dt.Ticks / TimeSpan.TicksPerMinute * TimeSpan.TicksPerMinute, dt.Kind);

        public DateTime CeilingToMinute()
        {
            var remainder = dt.Ticks % TimeSpan.TicksPerMinute;
            return remainder == 0
                ? dt
                : new DateTime(dt.Ticks + (TimeSpan.TicksPerMinute - remainder), dt.Kind);
        }
    }
}
