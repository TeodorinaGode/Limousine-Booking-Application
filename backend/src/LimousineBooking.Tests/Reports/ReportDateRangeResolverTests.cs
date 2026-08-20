using LimousineBooking.Application.Common;
using Xunit;

namespace LimousineBooking.Tests.Reports;

public class ReportDateRangeResolverTests
{
    // 2026-09-15 12:00 UTC is 2026-09-15 14:00 in Zurich (CEST, UTC+2).
    private static readonly DateTime FixedUtcNow = new(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Resolve_WithNoDates_DefaultsToCurrentZurichMonth()
    {
        var (range, error) = ReportDateRangeResolver.Resolve(null, null, FixedUtcNow);

        Assert.Null(error);
        Assert.Equal(new DateOnly(2026, 9, 1), range!.FromLocal);
        Assert.Equal(new DateOnly(2026, 9, 15), range.ToLocal);
    }

    [Fact]
    public void Resolve_ComputesHalfOpenUtcRange_NotIncludingNextDay()
    {
        var (range, _) = ReportDateRangeResolver.Resolve(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), FixedUtcNow);

        // 2026-09-01 00:00 Zurich (CEST, UTC+2) = 2026-08-31 22:00 UTC.
        Assert.Equal(new DateTime(2026, 8, 31, 22, 0, 0, DateTimeKind.Utc), range!.FromUtc);
        // Exclusive upper bound: 2026-09-02 00:00 Zurich = 2026-09-01 22:00 UTC.
        Assert.Equal(new DateTime(2026, 9, 1, 22, 0, 0, DateTimeKind.Utc), range.ToUtcExclusive);
    }

    [Fact]
    public void Resolve_DateFromAfterDateTo_ReturnsError()
    {
        var (range, error) = ReportDateRangeResolver.Resolve(new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 1), FixedUtcNow);

        Assert.Null(range);
        Assert.Equal("dateFrom must not be after dateTo.", error);
    }

    [Fact]
    public void Resolve_RangeExceedingMaximum_ReturnsError()
    {
        var (range, error) = ReportDateRangeResolver.Resolve(new DateOnly(2020, 1, 1), new DateOnly(2026, 9, 15), FixedUtcNow);

        Assert.Null(range);
        Assert.NotNull(error);
        Assert.Contains("366", error);
    }

    [Fact]
    public void Resolve_MaximumAllowedRange_Succeeds()
    {
        var (range, error) = ReportDateRangeResolver.Resolve(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), FixedUtcNow);

        Assert.Null(error);
        Assert.NotNull(range);
    }

    [Fact]
    public void Resolve_SameDayRange_Succeeds()
    {
        var (range, error) = ReportDateRangeResolver.Resolve(new DateOnly(2026, 9, 15), new DateOnly(2026, 9, 15), FixedUtcNow);

        Assert.Null(error);
        Assert.Equal(range!.FromLocal, range.ToLocal);
    }
}
