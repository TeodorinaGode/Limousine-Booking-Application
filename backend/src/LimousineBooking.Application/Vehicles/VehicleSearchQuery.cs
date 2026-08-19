namespace LimousineBooking.Application.Vehicles;

/// <summary>
/// Query parameters for GET /api/admin/vehicles. Bound from the query string.
/// </summary>
public class VehicleSearchQuery
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    /// <summary>Matches RegistrationNumber, Make, Model, or VehicleType (case-insensitive, substring).</summary>
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>Only vehicles with PassengerCapacity greater than or equal to this value.</summary>
    public int? MinCapacity { get; set; }

    /// <summary>
    /// One of: registrationNumber, make, model, passengerCapacity, createdAt.
    /// Unknown values fall back to "registrationNumber" — never used to build raw SQL.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>"asc" (default) or "desc".</summary>
    public string? SortDirection { get; set; }

    private int _page = 1;
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    private int _pageSize = DefaultPageSize;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => value
        };
    }
}
