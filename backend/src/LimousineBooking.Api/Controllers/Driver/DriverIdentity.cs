using LimousineBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LimousineBooking.Api.Controllers.Driver;

/// <summary>Shared "resolve the authenticated driver's own id from the JWT" helper for the driver controllers — mirrors AvailabilityController's own copy.</summary>
internal static class DriverIdentity
{
    public static bool TryGetDriverId(ICurrentUserService currentUser, out Guid driverId, out ActionResult? problem)
    {
        if (currentUser.DriverId.HasValue)
        {
            driverId = currentUser.DriverId.Value;
            problem = null;
            return true;
        }

        driverId = Guid.Empty;
        // A Driver-role token should always carry a driverId claim (see
        // JwtTokenService/LoginHandler) — reaching here means an inconsistent
        // token, not a normal user-facing error.
        problem = new ObjectResult(new { message = "Driver identity missing from token." }) { StatusCode = 500 };
        return false;
    }
}
