using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

/// <summary>
/// A person at a partner company (<see cref="CrmCompany"/>) — e.g. a hotel
/// concierge manager or corporate travel coordinator. Distinct from a
/// customer who placed an individual <see cref="Booking"/>; a contact can be
/// linked to more than one company via <see cref="CrmCompanyContact"/>.
/// </summary>
public class CrmContact : AuditableEntity
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string? JobTitle { get; private set; }
    public string? Phone { get; private set; }

    /// <summary>"Phone" or "Email" — same free-form convention as <see cref="ContactMessage.PreferredContactMethod"/>.</summary>
    public string? PreferredContactMethod { get; private set; }

    public CrmLifecycleStatus Status { get; private set; } = CrmLifecycleStatus.NewProspect;
    public Guid? AssignedOwnerId { get; private set; }
    public DateTime? LastContactedAt { get; private set; }
    public DateTime? NextFollowUpAt { get; private set; }
    public string? Notes { get; private set; }
    public bool IsArchived { get; private set; }

    public User? AssignedOwner { get; private set; }
    public ICollection<CrmCompanyContact> CompanyContacts { get; private set; } = new List<CrmCompanyContact>();
    public ICollection<CrmDealContact> DealContacts { get; private set; } = new List<CrmDealContact>();
    public ICollection<CrmActivity> Activities { get; private set; } = new List<CrmActivity>();
    public ICollection<CrmTask> Tasks { get; private set; } = new List<CrmTask>();

    private static readonly string[] AllowedContactMethods = { "Phone", "Email" };

    private CrmContact()
    {
    }

    public CrmContact(string firstName, string lastName, string email)
    {
        ValidateCore(firstName, lastName, email);
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim();
    }

    public void UpdateDetails(
        string firstName,
        string lastName,
        string email,
        string? jobTitle,
        string? phone,
        string? preferredContactMethod,
        CrmLifecycleStatus status,
        Guid? assignedOwnerId,
        string? notes)
    {
        ValidateCore(firstName, lastName, email);
        if (!string.IsNullOrWhiteSpace(phone) && !PhoneFormat.IsValid(phone))
            throw new ArgumentException("Phone format is invalid.", nameof(phone));
        if (!string.IsNullOrWhiteSpace(preferredContactMethod) && !AllowedContactMethods.Contains(preferredContactMethod, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Preferred contact method must be 'Phone' or 'Email'.", nameof(preferredContactMethod));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim();
        JobTitle = Normalize(jobTitle);
        Phone = Normalize(phone);
        PreferredContactMethod = Normalize(preferredContactMethod);
        Status = status;
        AssignedOwnerId = assignedOwnerId;
        Notes = Normalize(notes);
    }

    public void RecordContact(DateTime contactedAtUtc) => LastContactedAt = contactedAtUtc;

    public void ScheduleFollowUp(DateTime? followUpAtUtc) => NextFollowUpAt = followUpAtUtc;

    public void Archive() => IsArchived = true;

    public void Unarchive() => IsArchived = false;

    private static void ValidateCore(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (!EmailFormat.IsValid(email))
            throw new ArgumentException("Email format is invalid.", nameof(email));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
