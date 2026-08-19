namespace LimousineBooking.Application.Interfaces;

/// <summary>
/// Runs an operation inside a Serializable database transaction, retrying it if
/// the database detects a serialization conflict with a concurrent transaction.
/// See AutomaticAssignmentService for why this is needed.
/// </summary>
public interface ITransactionRunner
{
    Task<T> RunSerializableAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
