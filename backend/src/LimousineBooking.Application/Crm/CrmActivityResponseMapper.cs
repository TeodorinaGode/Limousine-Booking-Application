using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;

namespace LimousineBooking.Application.Crm;

/// <summary>Shared CrmActivity → CrmActivityResponse mapping, used by every place that surfaces an activity list (search, dashboard, company/contact/deal detail "Recent Activity") so EmailThreadId resolution and every other field stay consistent across all of them.</summary>
internal static class CrmActivityResponseMapper
{
    public static readonly IReadOnlyDictionary<Guid, Guid> EmptyThreadIdLookup = new Dictionary<Guid, Guid>();

    public static async Task<IReadOnlyList<CrmActivityResponse>> ToResponsesAsync(
        IReadOnlyList<CrmActivity> activities, ICrmEmailMessageRepository emailMessageRepository, CancellationToken cancellationToken)
    {
        var messageIds = activities.Where(a => a.EmailMessageId.HasValue).Select(a => a.EmailMessageId!.Value).Distinct().ToList();
        var threadIdsByMessageId = await emailMessageRepository.GetThreadIdsByMessageIdsAsync(messageIds, cancellationToken);
        return activities.Select(a => ToResponse(a, threadIdsByMessageId)).ToList();
    }

    /// <summary>loggedByOverride is for the just-created-this-request case, where activity.User isn't loaded (no navigation on a freshly constructed entity) — every other caller leaves it null and relies on activity.User from its own eager-loaded query.</summary>
    public static CrmActivityResponse ToResponse(CrmActivity activity, IReadOnlyDictionary<Guid, Guid> threadIdsByMessageId, User? loggedByOverride = null)
    {
        var loggedBy = loggedByOverride ?? activity.User;
        return new CrmActivityResponse
        {
            Id = activity.Id,
            ActivityType = activity.ActivityType.ToString(),
            Subject = activity.Subject,
            Description = activity.Description,
            OccurredAt = activity.OccurredAt,
            LoggedBy = loggedBy is null
                ? new CrmOwnerSummary()
                : new CrmOwnerSummary { Id = loggedBy.Id, FirstName = loggedBy.FirstName, LastName = loggedBy.LastName, Email = loggedBy.Email },
            CompanyId = activity.CompanyId,
            CompanyName = activity.Company?.Name,
            ContactId = activity.ContactId,
            ContactName = activity.Contact is null ? null : $"{activity.Contact.FirstName} {activity.Contact.LastName}",
            DealId = activity.DealId,
            DealName = activity.Deal?.Name,
            Outcome = activity.Outcome,
            CreatedAt = activity.CreatedAt,
            EmailMessageId = activity.EmailMessageId,
            EmailThreadId = activity.EmailMessageId.HasValue && threadIdsByMessageId.TryGetValue(activity.EmailMessageId.Value, out var threadId) ? threadId : null
        };
    }
}
