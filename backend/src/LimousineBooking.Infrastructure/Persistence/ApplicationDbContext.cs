using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for domain entities will be added as they are introduced
    // in subsequent steps.
}
