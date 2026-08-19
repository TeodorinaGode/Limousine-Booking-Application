using System.Data;
using LimousineBooking.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LimousineBooking.Infrastructure.Persistence;

/// <summary>
/// Runs an operation inside a PostgreSQL Serializable transaction, retrying on a
/// serialization failure (SQLSTATE 40001) or deadlock (40P01). Under Serializable
/// isolation, Postgres guarantees the transaction behaves as if it ran alone; if a
/// concurrent transaction commits conflicting state in the meantime, one of the two
/// is aborted with one of those SQLSTATEs rather than silently corrupting data — the
/// retry re-reads the (now up to date) state and reaches a correct outcome.
/// </summary>
public class TransactionRunner : ITransactionRunner
{
    private const int MaxAttempts = 3;

    private readonly ApplicationDbContext _dbContext;

    public TransactionRunner(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> RunSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception ex) when (attempt < MaxAttempts && IsSerializationFailure(ex))
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        }
    }

    private static bool IsSerializationFailure(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current is PostgresException pg && (pg.SqlState == "40001" || pg.SqlState == "40P01"))
                return true;
        }

        return false;
    }
}
