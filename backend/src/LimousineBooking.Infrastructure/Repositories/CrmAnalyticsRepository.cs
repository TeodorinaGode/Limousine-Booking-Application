using LimousineBooking.Application.Common;
using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Enums;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class CrmAnalyticsRepository : ICrmAnalyticsRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CrmAnalyticsRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CrmDashboardResponse> GetDashboardAggregatesAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var todayLocal = DateOnly.FromDateTime(SwissTimeZone.ConvertFromUtc(nowUtc));
        var monthStartUtc = ToUtc(new DateOnly(todayLocal.Year, todayLocal.Month, 1));
        var monthEndExclusiveUtc = ToUtc(new DateOnly(todayLocal.Year, todayLocal.Month, 1).AddMonths(1));
        var followUpHorizonUtc = nowUtc.AddDays(7);
        var today = DateOnly.FromDateTime(nowUtc);

        var totalCompanies = await _dbContext.CrmCompanies.CountAsync(c => !c.IsArchived, cancellationToken);
        var totalContacts = await _dbContext.CrmContacts.CountAsync(c => !c.IsArchived, cancellationToken);

        var companiesContactedThisMonth = await _dbContext.CrmCompanies
            .CountAsync(c => !c.IsArchived && c.LastContactedAt != null && c.LastContactedAt >= monthStartUtc && c.LastContactedAt < monthEndExclusiveUtc, cancellationToken);

        var companiesNeedingFollowUp = await _dbContext.CrmCompanies
            .CountAsync(c => !c.IsArchived && c.NextFollowUpAt != null && c.NextFollowUpAt <= nowUtc, cancellationToken);
        var contactsNeedingFollowUp = await _dbContext.CrmContacts
            .CountAsync(c => !c.IsArchived && c.NextFollowUpAt != null && c.NextFollowUpAt <= nowUtc, cancellationToken);

        var companiesUpcomingFollowUp = await _dbContext.CrmCompanies
            .CountAsync(c => !c.IsArchived && c.NextFollowUpAt != null && c.NextFollowUpAt > nowUtc && c.NextFollowUpAt <= followUpHorizonUtc, cancellationToken);
        var contactsUpcomingFollowUp = await _dbContext.CrmContacts
            .CountAsync(c => !c.IsArchived && c.NextFollowUpAt != null && c.NextFollowUpAt > nowUtc && c.NextFollowUpAt <= followUpHorizonUtc, cancellationToken);

        var openDeals = await _dbContext.CrmDeals.CountAsync(d => !d.IsArchived && !d.Stage!.IsWon && !d.Stage!.IsLost, cancellationToken);
        var wonDeals = await _dbContext.CrmDeals.CountAsync(d => !d.IsArchived && d.Stage!.IsWon, cancellationToken);
        var lostDeals = await _dbContext.CrmDeals.CountAsync(d => !d.IsArchived && d.Stage!.IsLost, cancellationToken);

        var openDealsQuery = _dbContext.CrmDeals.Where(d => !d.IsArchived && !d.Stage!.IsWon && !d.Stage!.IsLost);
        var openPipelineValue = await openDealsQuery.SumAsync(d => (decimal?)d.Value, cancellationToken) ?? 0m;
        var weightedPipelineValue = await openDealsQuery.SumAsync(d => (decimal?)(d.Value * d.Probability / 100m), cancellationToken) ?? 0m;

        var tasksDueToday = await _dbContext.CrmTasks
            .CountAsync(t => t.DueDate == today && t.Status != CrmTaskStatus.Completed && t.Status != CrmTaskStatus.Cancelled, cancellationToken);
        var overdueTasks = await _dbContext.CrmTasks
            .CountAsync(t => t.DueDate < today && t.Status != CrmTaskStatus.Completed && t.Status != CrmTaskStatus.Cancelled, cancellationToken);

        return new CrmDashboardResponse
        {
            TotalCompanies = totalCompanies,
            TotalContacts = totalContacts,
            CompaniesContactedThisMonth = companiesContactedThisMonth,
            ContactsAndCompaniesNeedingFollowUp = companiesNeedingFollowUp + contactsNeedingFollowUp,
            OpenDeals = openDeals,
            WonDeals = wonDeals,
            LostDeals = lostDeals,
            OpenPipelineValue = openPipelineValue,
            WeightedPipelineValue = weightedPipelineValue,
            TasksDueToday = tasksDueToday,
            OverdueTasks = overdueTasks,
            UpcomingFollowUps = companiesUpcomingFollowUp + contactsUpcomingFollowUp
        };
    }

    public async Task<CrmReportSummaryResponse> GetReportAggregatesAsync(ResolvedReportDateRange range, CancellationToken cancellationToken = default)
    {
        var newCompanies = await _dbContext.CrmCompanies
            .CountAsync(c => c.CreatedAt >= range.FromUtc && c.CreatedAt < range.ToUtcExclusive, cancellationToken);
        var newContacts = await _dbContext.CrmContacts
            .CountAsync(c => c.CreatedAt >= range.FromUtc && c.CreatedAt < range.ToUtcExclusive, cancellationToken);
        var activitiesLogged = await _dbContext.CrmActivities
            .CountAsync(a => a.OccurredAt >= range.FromUtc && a.OccurredAt < range.ToUtcExclusive, cancellationToken);
        var tasksCompleted = await _dbContext.CrmTasks
            .CountAsync(t => t.CompletedAt != null && t.CompletedAt >= range.FromUtc && t.CompletedAt < range.ToUtcExclusive, cancellationToken);

        var closedInRange = _dbContext.CrmDeals
            .Where(d => d.ClosedAt != null && d.ClosedAt >= range.FromUtc && d.ClosedAt < range.ToUtcExclusive);

        var dealsWon = await closedInRange.CountAsync(d => d.Stage!.IsWon, cancellationToken);
        var dealsLost = await closedInRange.CountAsync(d => d.Stage!.IsLost, cancellationToken);
        var wonValue = await closedInRange.Where(d => d.Stage!.IsWon).SumAsync(d => (decimal?)d.Value, cancellationToken) ?? 0m;

        decimal? conversionRate = dealsWon + dealsLost > 0
            ? Math.Round(100m * dealsWon / (dealsWon + dealsLost), 1)
            : null;

        var openDealsByStage = await _dbContext.CrmDeals
            .Where(d => !d.IsArchived && !d.Stage!.IsWon && !d.Stage!.IsLost)
            .GroupBy(d => new { d.Stage!.Name, d.Stage!.SortOrder })
            .Select(g => new CrmDealsByStageItem
            {
                StageName = g.Key.Name,
                SortOrder = g.Key.SortOrder,
                DealCount = g.Count(),
                TotalValue = g.Sum(d => d.Value)
            })
            .OrderBy(item => item.SortOrder)
            .ToListAsync(cancellationToken);

        return new CrmReportSummaryResponse
        {
            DateFrom = range.FromLocal,
            DateTo = range.ToLocal,
            NewCompanies = newCompanies,
            NewContacts = newContacts,
            ActivitiesLogged = activitiesLogged,
            TasksCompleted = tasksCompleted,
            DealsWon = dealsWon,
            DealsLost = dealsLost,
            WonValue = wonValue,
            ConversionRatePercent = conversionRate,
            OpenDealsByStage = openDealsByStage
        };
    }

    private static DateTime ToUtc(DateOnly localDate) =>
        TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified), SwissTimeZone.Instance);
}
