using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LimousineBooking.Infrastructure.Repositories;

public class DriverRepository : IDriverRepository
{
    private readonly ApplicationDbContext _dbContext;

    public DriverRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Driver?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _dbContext.Drivers.SingleOrDefaultAsync(d => d.UserId == userId, cancellationToken);
}
