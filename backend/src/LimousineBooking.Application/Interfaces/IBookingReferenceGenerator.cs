namespace LimousineBooking.Application.Interfaces;

/// <summary>Produces a unique, customer-facing booking reference (e.g. "LM-20260819-483920").</summary>
public interface IBookingReferenceGenerator
{
    Task<string> GenerateAsync(DateOnly travelDate, CancellationToken cancellationToken = default);
}
