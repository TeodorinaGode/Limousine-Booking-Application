namespace LimousineBooking.Application.Reports;

/// <summary>Wraps a report response with the resolved date range, or a validation error (400).</summary>
public class ReportResult<T>
{
    public bool Succeeded { get; }
    public T? Value { get; }
    public string? Error { get; }

    private ReportResult(bool succeeded, T? value, string? error)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
    }

    public static ReportResult<T> Success(T value) => new(true, value, null);

    public static ReportResult<T> Failure(string error) => new(false, default, error);
}
