namespace LimousineBooking.Application.Common;

/// <summary>
/// A resolved report date range: the inclusive local (Europe/Zurich) boundary
/// dates as entered/defaulted, plus the equivalent half-open UTC instant range
/// ([FromUtc, ToUtcExclusive)) for querying timestamptz columns — computed once
/// here so no call site ever writes its own AddDays(1).AddTicks(-1) arithmetic.
/// </summary>
public class ResolvedReportDateRange
{
    public DateOnly FromLocal { get; init; }
    public DateOnly ToLocal { get; init; }
    public DateTime FromUtc { get; init; }
    public DateTime ToUtcExclusive { get; init; }
}

/// <summary>
/// The single place report date filters are validated and resolved — every
/// reporting endpoint shares this one dateFrom/dateTo convention (section 4)
/// instead of each report inventing its own date-filter shape.
/// </summary>
public static class ReportDateRangeResolver
{
    public const int MaxRangeDays = 366;

    /// <summary>
    /// Resolves dateFrom/dateTo (both optional — defaults to the current
    /// Europe/Zurich month) against <paramref name="utcNow"/>. Returns an error
    /// message instead of throwing so the caller can return a 400 with a clear reason.
    /// </summary>
    public static (ResolvedReportDateRange? Range, string? Error) Resolve(DateOnly? dateFrom, DateOnly? dateTo, DateTime utcNow)
    {
        var todayLocal = DateOnly.FromDateTime(SwissTimeZone.ConvertFromUtc(utcNow));
        var fromLocal = dateFrom ?? new DateOnly(todayLocal.Year, todayLocal.Month, 1);
        var toLocal = dateTo ?? todayLocal;

        if (fromLocal > toLocal)
            return (null, "dateFrom must not be after dateTo.");

        if (toLocal.DayNumber - fromLocal.DayNumber > MaxRangeDays)
            return (null, $"The date range must not exceed {MaxRangeDays} days.");

        return (new ResolvedReportDateRange
        {
            FromLocal = fromLocal,
            ToLocal = toLocal,
            FromUtc = ToUtc(fromLocal),
            ToUtcExclusive = ToUtc(toLocal.AddDays(1))
        }, null);
    }

    private static DateTime ToUtc(DateOnly localDate) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), SwissTimeZone.Instance);
}
