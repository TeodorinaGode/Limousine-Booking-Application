using System.Text.Json;
using LimousineBooking.Application.Drivers;

namespace LimousineBooking.Tests.Drivers;

public class DriverResponseShapeTests
{
    [Fact]
    public void DriverResponse_SerializedJson_NeverContainsPassword()
    {
        var response = new DriverResponse
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Smith",
            Email = "john.smith@example.com",
            Phone = "+41791234567",
            IsActive = true,
            IsAvailable = false,
            Vehicle = new DriverVehicleSummary
            {
                Id = Guid.NewGuid(),
                RegistrationNumber = "BS 123456",
                Make = "Mercedes-Benz",
                Model = "V-Class"
            }
        };

        var json = JsonSerializer.Serialize(response);

        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase);
    }
}
