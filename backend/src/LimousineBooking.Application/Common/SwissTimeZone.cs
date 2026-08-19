namespace LimousineBooking.Application.Common;

/// <summary>
/// All customer-facing booking dates/times (<see cref="System.DateOnly"/> +
/// <see cref="System.TimeOnly"/>) are entered and displayed in Europe/Zurich
/// local time. This wraps the one <see cref="TimeZoneInfo"/> lookup so the
/// IANA id isn't repeated at every call site.
/// </summary>
public static class SwissTimeZone
{
    public static TimeZoneInfo Instance { get; } = TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich");

    public static DateTime ConvertFromUtc(DateTime utcDateTime) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc), Instance);
}
