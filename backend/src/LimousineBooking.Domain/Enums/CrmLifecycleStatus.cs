namespace LimousineBooking.Domain.Enums;

/// <summary>Shared lifecycle-status vocabulary for both <see cref="Entities.CrmCompany"/> and <see cref="Entities.CrmContact"/>.</summary>
public enum CrmLifecycleStatus
{
    NewProspect,
    Researching,
    Contacted,
    InDiscussion,
    Qualified,
    Customer,
    Inactive,
    NotInterested
}
