namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Testable substitute for <see cref="System.DateTime.UtcNow"/> — lets lead-time
/// and past-date validation be exercised with a fixed "now" in unit tests.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
