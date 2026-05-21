using NodaTime;

namespace TaskApp.Infrastructure.Utils;

public static class PanamaTimeZone
{
    private const string IanaTimeZone = "America/Panama";
    private const string WindowsTimeZone = "SA Pacific Standard Time";
    private static readonly DateTimeZone PanamaZone = DateTimeZoneProviders.Tzdb["America/Panama"];

    public static DateTime GetCurrentPanamaDateTime()
    {
        var utcNow = DateTime.UtcNow;

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(IanaTimeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            var fallback = TimeZoneInfo.FindSystemTimeZoneById(WindowsTimeZone);
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, fallback);
        }
    }

    public static DateTime GetCurrentPanamaDate()
    {
        return GetCurrentPanamaDateTime().Date;
    }

    public static TimeSpan GetPanamaOffset()
    {
        var instant = SystemClock.Instance.GetCurrentInstant();
        var offset = PanamaZone.GetUtcOffset(instant);
        return offset.ToTimeSpan();
    }
}
