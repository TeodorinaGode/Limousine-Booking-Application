using System.Security.Cryptography;
using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Application.Bookings;

/// <summary>
/// Format: "LM-{travelDate:yyyyMMdd}-{6-digit random}", e.g. "LM-20260819-483920".
/// The random suffix (rather than a sequential counter) avoids a shared counter
/// needing coordination/locking across concurrent anonymous submissions; the
/// bounded retry loop against the uniqueness index handles the rare collision.
/// </summary>
public class BookingReferenceGenerator : IBookingReferenceGenerator
{
    private const int MaxAttempts = 10;

    private readonly IBookingRepository _bookingRepository;

    public BookingReferenceGenerator(IBookingRepository bookingRepository)
    {
        _bookingRepository = bookingRepository;
    }

    public async Task<string> GenerateAsync(DateOnly travelDate, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidate = $"LM-{travelDate:yyyyMMdd}-{RandomNumberGenerator.GetInt32(0, 1_000_000):D6}";

            if (!await _bookingRepository.ReferenceExistsAsync(candidate, cancellationToken))
                return candidate;
        }

        throw new InvalidOperationException($"Could not generate a unique booking reference after {MaxAttempts} attempts.");
    }
}
