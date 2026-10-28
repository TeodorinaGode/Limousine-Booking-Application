using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// A potential or existing B2B partner (hotel, travel agency, corporate
/// client, event/wedding organizer, tourism partner, concierge service) —
/// distinct from an individual <see cref="Booking"/> customer. Deals,
/// contacts, activities, and tasks attach to a company; winning a deal here
/// never creates a booking automatically.
/// </summary>
public class CrmCompany : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public CrmCompanyType CompanyType { get; private set; }
    public string? Website { get; private set; }
    public string? Domain { get; private set; }
    public string? Industry { get; private set; }
    public string? Country { get; private set; }
    public string? City { get; private set; }
    public string? Address { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public CrmLifecycleStatus Status { get; private set; } = CrmLifecycleStatus.NewProspect;
    public CrmPriority Priority { get; private set; } = CrmPriority.Normal;
    public Guid? AssignedOwnerId { get; private set; }
    public string? LeadSource { get; private set; }
    public DateTime? LastContactedAt { get; private set; }
    public DateTime? NextFollowUpAt { get; private set; }
    public string? Notes { get; private set; }
    public bool IsArchived { get; private set; }

    public User? AssignedOwner { get; private set; }
    public ICollection<CrmCompanyContact> CompanyContacts { get; private set; } = new List<CrmCompanyContact>();
    public ICollection<CrmDeal> Deals { get; private set; } = new List<CrmDeal>();
    public ICollection<CrmActivity> Activities { get; private set; } = new List<CrmActivity>();
    public ICollection<CrmTask> Tasks { get; private set; } = new List<CrmTask>();

    private CrmCompany()
    {
    }

    public CrmCompany(string name, CrmCompanyType companyType)
    {
        ValidateName(name);
        Name = name.Trim();
        CompanyType = companyType;
    }

    public void UpdateDetails(
        string name,
        CrmCompanyType companyType,
        string? website,
        string? domain,
        string? industry,
        string? country,
        string? city,
        string? address,
        string? phone,
        string? email,
        CrmLifecycleStatus status,
        CrmPriority priority,
        Guid? assignedOwnerId,
        string? leadSource,
        string? notes)
    {
        ValidateName(name);
        if (!string.IsNullOrWhiteSpace(email) && !EmailFormat.IsValid(email))
            throw new ArgumentException("Email format is invalid.", nameof(email));
        if (!string.IsNullOrWhiteSpace(phone) && !PhoneFormat.IsValid(phone))
            throw new ArgumentException("Phone format is invalid.", nameof(phone));

        Name = name.Trim();
        CompanyType = companyType;
        Website = Normalize(website);
        Domain = Normalize(domain);
        Industry = Normalize(industry);
        Country = Normalize(country);
        City = Normalize(city);
        Address = Normalize(address);
        Phone = Normalize(phone);
        Email = Normalize(email);
        Status = status;
        Priority = priority;
        AssignedOwnerId = assignedOwnerId;
        LeadSource = Normalize(leadSource);
        Notes = Normalize(notes);
    }

    public void RecordContact(DateTime contactedAtUtc) => LastContactedAt = contactedAtUtc;

    public void ScheduleFollowUp(DateTime? followUpAtUtc) => NextFollowUpAt = followUpAtUtc;

    public void Archive() => IsArchived = true;

    public void Unarchive() => IsArchived = false;

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
