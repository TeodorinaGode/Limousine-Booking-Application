using LimousineBooking.Domain.Common;
using LimousineBooking.Domain.Enums;

namespace LimousineBooking.Domain.Entities;

public class User : AuditableEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// The user's saved language preference (en/de/fr) — null means "no preference
    /// saved yet, use the browser's language" (section 20: language selection is never
    /// required at account creation). Once set, this drives both the UI on login and
    /// which language this user's emails are rendered in.
    /// </summary>
    public string? LanguageCode { get; private set; }

    public Driver? Driver { get; private set; }

    private User()
    {
    }

    public User(string email, string passwordHash, string firstName, string lastName, UserRole role)
    {
        ValidateProfile(email, firstName, lastName);
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
    }

    /// <summary>
    /// Updates the profile fields an administrator can edit. Never changes
    /// <see cref="Role"/> or <see cref="PasswordHash"/> — use <see cref="SetPasswordHash"/>
    /// for password resets.
    /// </summary>
    public void UpdateProfile(string email, string firstName, string lastName)
    {
        ValidateProfile(email, firstName, lastName);

        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        PasswordHash = passwordHash;
    }

    /// <summary>Null clears the preference (back to "use the browser's language"); a non-null value is normalized via <see cref="Common.SupportedLanguages.Normalize"/> — an unsupported code silently becomes "en" rather than being rejected.</summary>
    public void SetLanguage(string? languageCode) =>
        LanguageCode = languageCode is null ? null : Common.SupportedLanguages.Normalize(languageCode);

    public void Deactivate() => IsActive = false;

    public void Activate() => IsActive = true;

    private static void ValidateProfile(string email, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (!EmailFormat.IsValid(email))
            throw new ArgumentException("Email format is invalid.", nameof(email));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
    }
}
