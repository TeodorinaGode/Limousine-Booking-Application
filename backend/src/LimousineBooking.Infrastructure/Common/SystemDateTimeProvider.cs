using LimousineBooking.Application.Interfaces;

namespace LimousineBooking.Infrastructure.Common;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
