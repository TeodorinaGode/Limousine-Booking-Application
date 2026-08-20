import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../../context/AuthContext";
import { getBookings } from "../../../services/adminBookingService";
import { getDrivers } from "../../../services/driverService";
import { getVehicles } from "../../../services/vehicleService";
import { getRoutes } from "../../../services/routeService";
import AdminNav from "../../../components/AdminNav";
import PageHeader from "../../../components/PageHeader";
import StatusBadge from "../../../components/StatusBadge";
import type { AdminBookingListItemDto } from "../../../types/adminBooking";
import type { DriverDto } from "../../../types/driver";
import type { VehicleDto } from "../../../types/vehicle";
import type { RouteDto } from "../../../types/route";

const PAGE_SIZE = 20;

const STATUS_OPTIONS: { value: string; label: string }[] = [
  { value: "Pending,Confirmed", label: "Active (Pending + Confirmed)" },
  { value: "", label: "All" },
  { value: "Pending", label: "Pending" },
  { value: "Confirmed", label: "Confirmed" },
  { value: "Cancelled", label: "Cancelled" },
  { value: "Completed", label: "Completed" },
];

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function addDaysIso(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toISOString().slice(0, 10);
}

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, { day: "2-digit", month: "short", year: "numeric" });
}

function formatTime(time: string): string {
  return time.slice(0, 5);
}

function BookingsPage() {
  const { accessToken } = useAuth();

  const [bookings, setBookings] = useState<AdminBookingListItemDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("Pending,Confirmed");
  const [assignmentFilter, setAssignmentFilter] = useState("all");
  const [paymentStatusFilter, setPaymentStatusFilter] = useState("all");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [driverId, setDriverId] = useState("");
  const [vehicleId, setVehicleId] = useState("");
  const [routeId, setRouteId] = useState("");
  const [sortBy, setSortBy] = useState("bookingDate");
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc");

  const [drivers, setDrivers] = useState<DriverDto[]>([]);
  const [vehicles, setVehicles] = useState<VehicleDto[]>([]);
  const [routes, setRoutes] = useState<RouteDto[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadBookings = useCallback(async () => {
    if (!accessToken) return;

    setIsLoading(true);
    setError(null);
    try {
      const result = await getBookings(
        {
          search,
          status: status || undefined,
          dateFrom: dateFrom || undefined,
          dateTo: dateTo || undefined,
          driverId: driverId || undefined,
          vehicleId: vehicleId || undefined,
          routeId: routeId || undefined,
          assignmentFilter,
          paymentStatus: paymentStatusFilter,
          sortBy,
          sortDirection,
          page,
          pageSize: PAGE_SIZE,
        },
        accessToken
      );
      setBookings(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load bookings.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, search, status, assignmentFilter, paymentStatusFilter, dateFrom, dateTo, driverId, vehicleId, routeId, sortBy, sortDirection, page]);

  useEffect(() => {
    loadBookings();
  }, [loadBookings]);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      try {
        const [driverResult, vehicleResult, routeResult] = await Promise.all([
          getDrivers({ isActive: true, pageSize: 100 }, accessToken),
          getVehicles({ isActive: true, pageSize: 100 }, accessToken),
          getRoutes({ isActive: true, pageSize: 100 }, accessToken),
        ]);
        setDrivers(driverResult.items);
        setVehicles(vehicleResult.items);
        setRoutes(routeResult.items);
      } catch {
        // Filter dropdowns are a convenience; failing to load them shouldn't block the list.
      }
    })();
  }, [accessToken]);

  useEffect(() => {
    const timeout = setTimeout(() => {
      setPage(1);
      setSearch(searchInput);
    }, 300);
    return () => clearTimeout(timeout);
  }, [searchInput]);

  const applyQuickDateFilter = (from: string, to: string) => {
    setPage(1);
    setDateFrom(from);
    setDateTo(to);
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
      <PageHeader title="Bookings" description="Manage customer reservations and upcoming trips." />

      <div className="row" style={{ marginBottom: "1rem" }}>
        <input
          type="search"
          placeholder="Search reference, name, email, phone..."
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          aria-label="Search bookings"
        />

        <label>
          Status:{" "}
          <select
            value={status}
            onChange={(e) => {
              setPage(1);
              setStatus(e.target.value);
            }}
          >
            {STATUS_OPTIONS.map((option) => (
              <option key={option.label} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>

        <label>
          Assignment:{" "}
          <select
            value={assignmentFilter}
            onChange={(e) => {
              setPage(1);
              setAssignmentFilter(e.target.value);
            }}
          >
            <option value="all">All</option>
            <option value="automatic">Automatically Assigned</option>
            <option value="manual">Manually Assigned</option>
            <option value="requiresManual">Requires Manual Assignment</option>
          </select>
        </label>

        <label>
          Payment:{" "}
          <select
            value={paymentStatusFilter}
            onChange={(e) => {
              setPage(1);
              setPaymentStatusFilter(e.target.value);
            }}
          >
            <option value="all">All</option>
            <option value="notStarted">Not Started</option>
            <option value="pending">Pending</option>
            <option value="processing">Processing</option>
            <option value="paid">Paid</option>
            <option value="failed">Failed</option>
            <option value="cancelled">Cancelled</option>
            <option value="refunded">Refunded</option>
          </select>
        </label>

        <label>
          Driver:{" "}
          <select
            value={driverId}
            onChange={(e) => {
              setPage(1);
              setDriverId(e.target.value);
            }}
          >
            <option value="">All drivers</option>
            {drivers.map((driver) => (
              <option key={driver.id} value={driver.id}>
                {driver.firstName} {driver.lastName}
              </option>
            ))}
          </select>
        </label>

        <label>
          Vehicle:{" "}
          <select
            value={vehicleId}
            onChange={(e) => {
              setPage(1);
              setVehicleId(e.target.value);
            }}
          >
            <option value="">All vehicles</option>
            {vehicles.map((vehicle) => (
              <option key={vehicle.id} value={vehicle.id}>
                {vehicle.make} {vehicle.model} - {vehicle.registrationNumber}
              </option>
            ))}
          </select>
        </label>

        <label>
          Route:{" "}
          <select
            value={routeId}
            onChange={(e) => {
              setPage(1);
              setRouteId(e.target.value);
            }}
          >
            <option value="">All routes</option>
            {routes.map((route) => (
              <option key={route.id} value={route.id}>
                {route.departureLocation} &rarr; {route.destination}
              </option>
            ))}
          </select>
        </label>
      </div>

      <div className="row" style={{ marginBottom: "1rem" }}>
        <label>
          From:{" "}
          <input
            type="date"
            aria-label="Date from"
            value={dateFrom}
            onChange={(e) => {
              setPage(1);
              setDateFrom(e.target.value);
            }}
          />
        </label>
        <label>
          To:{" "}
          <input
            type="date"
            aria-label="Date to"
            value={dateTo}
            onChange={(e) => {
              setPage(1);
              setDateTo(e.target.value);
            }}
          />
        </label>
        <button type="button" onClick={() => applyQuickDateFilter(todayIso(), todayIso())}>
          Today
        </button>
        <button type="button" onClick={() => applyQuickDateFilter(addDaysIso(1), addDaysIso(1))}>
          Tomorrow
        </button>
        <button type="button" onClick={() => applyQuickDateFilter(todayIso(), addDaysIso(7))}>
          Next 7 days
        </button>
        <button type="button" onClick={() => applyQuickDateFilter("", "")}>
          Clear dates
        </button>

        <label>
          Sort by:{" "}
          <select value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
            <option value="bookingDate">Booking date</option>
            <option value="createdAt">Created date</option>
            <option value="customerName">Customer name</option>
            <option value="status">Status</option>
          </select>
        </label>
        <button type="button" onClick={() => setSortDirection((d) => (d === "asc" ? "desc" : "asc"))}>
          {sortDirection === "asc" ? "Ascending ↑" : "Descending ↓"}
        </button>

        <button type="button" onClick={() => loadBookings()}>
          Refresh
        </button>
      </div>

      {error && <p role="alert">{error}</p>}

      {isLoading ? (
        <div className="stack">
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
        </div>
      ) : bookings.length === 0 ? (
        <div className="empty-state">
          <p className="empty-state__title">No Bookings Found</p>
          <p>Try adjusting your filters.</p>
        </div>
      ) : (
        <div style={{ overflowX: "auto" }}>
        <table>
          <thead>
            <tr>
              <th>Booking</th>
              <th>Customer</th>
              <th>Route</th>
              <th>Date</th>
              <th>Time</th>
              <th>Passengers</th>
              <th>Price</th>
              <th>Status</th>
              <th>Ride Status</th>
              <th>Payment</th>
              <th>Driver</th>
              <th>Vehicle</th>
              <th>Assignment</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {bookings.map((booking) => (
              <tr key={booking.id}>
                <td>{booking.bookingReference}</td>
                <td>
                  {booking.customerFirstName} {booking.customerLastName}
                </td>
                <td>
                  {booking.route.departureLocation} &rarr; {booking.route.destination}
                </td>
                <td>{formatDate(booking.bookingDate)}</td>
                <td>{formatTime(booking.pickupTime)}</td>
                <td>{booking.passengerCount}</td>
                <td>
                  {booking.price.toFixed(2)} {booking.currency}
                </td>
                <td>
                  <StatusBadge status={booking.status} />
                </td>
                <td>
                  <StatusBadge status={booking.rideStatus} />
                </td>
                <td>
                  <StatusBadge status={booking.paymentStatus} />
                </td>
                <td>{booking.driverName ?? "—"}</td>
                <td>{booking.vehicleDescription ?? "—"}</td>
                <td>{booking.assignment}</td>
                <td>
                  <Link to={`/admin/bookings/${booking.id}`}>View</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>
      )}

      {totalPages > 1 && (
        <div className="row" style={{ marginTop: "1rem" }}>
          <button type="button" className="btn-secondary" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
            Previous
          </button>
          <span>
            Page {page} of {totalPages}
          </span>
          <button type="button" className="btn-secondary" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
            Next
          </button>
        </div>
      )}
      </main>
    </div>
  );
}

export default BookingsPage;
