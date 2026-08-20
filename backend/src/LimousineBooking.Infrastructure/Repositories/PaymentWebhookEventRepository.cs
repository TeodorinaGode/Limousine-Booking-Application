using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LimousineBooking.Infrastructure.Repositories;

public class PaymentWebhookEventRepository : IPaymentWebhookEventRepository
{
    private const string UniqueViolationSqlState = "23505";

    private readonly ApplicationDbContext _dbContext;

    public PaymentWebhookEventRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(PaymentWebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        _dbContext.PaymentWebhookEvents.Add(webhookEvent);
        return Task.CompletedTask;
    }

    public bool IsDuplicateEventError(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException pg && pg.SqlState == UniqueViolationSqlState && pg.ConstraintName?.Contains("ProviderEventId", StringComparison.OrdinalIgnoreCase) == true)
                return true;
        }

        return false;
    }
}
