using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Persistence.Seed;

/// <summary>
/// Creates development-only Administrator/Driver login accounts if they don't
/// already exist. Only ever invoked when the host environment is Development
/// (see Program.cs) — never runs in Staging/Production.
/// </summary>
public static class DevelopmentDataSeeder
{
    // Development-only credential, intentionally committed to source and
    // documented in the README. Never used for production accounts.
    public const string DevelopmentPassword = "Dev#Passw0rd!";

    public static async Task SeedAsync(ApplicationDbContext dbContext, IPasswordService passwordService, CancellationToken cancellationToken = default)
    {
        await SeedUserAsync(dbContext, passwordService, "admin@example.com", "Admin", "User", UserRole.Administrator, cancellationToken);
        var driverUser = await SeedUserAsync(dbContext, passwordService, "driver@example.com", "Dev", "Driver", UserRole.Driver, cancellationToken);

        var existingDriver = await dbContext.Drivers.SingleOrDefaultAsync(d => d.UserId == driverUser.Id, cancellationToken);
        if (existingDriver is null)
        {
            var driver = new Driver(driverUser.Id, "+41000000000");
            dbContext.Drivers.Add(driver);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<User> SeedUserAsync(
        ApplicationDbContext dbContext,
        IPasswordService passwordService,
        string email,
        string firstName,
        string lastName,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (existing is not null)
            return existing;

        var passwordHash = passwordService.Hash(DevelopmentPassword);
        var user = new User(email, passwordHash, firstName, lastName, role);
        dbContext.Users.Add(user);

        return user;
    }
}
