using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Interfaces;
using Moq;
using Xunit;

namespace LimousineBooking.Tests.Bookings;

public class BookingReferenceGeneratorTests
{
    private readonly Mock<IBookingRepository> _bookingRepository = new();

    private BookingReferenceGenerator CreateGenerator() => new(_bookingRepository.Object);

    [Fact]
    public async Task GenerateAsync_ProducesReferenceContainingTravelDate()
    {
        _bookingRepository.Setup(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var reference = await CreateGenerator().GenerateAsync(new DateOnly(2026, 9, 10));

        Assert.StartsWith("LM-20260910-", reference);
        _bookingRepository.Verify(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_RetriesOnCollision_UntilUniqueReferenceFound()
    {
        _bookingRepository.SetupSequence(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        var reference = await CreateGenerator().GenerateAsync(new DateOnly(2026, 9, 10));

        Assert.StartsWith("LM-20260910-", reference);
        _bookingRepository.Verify(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GenerateAsync_ExhaustingAllAttempts_Throws()
    {
        _bookingRepository.Setup(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateGenerator().GenerateAsync(new DateOnly(2026, 9, 10)));
    }
}
