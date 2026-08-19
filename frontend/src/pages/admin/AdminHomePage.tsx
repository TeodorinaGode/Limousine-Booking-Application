import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getDashboard } from "../../services/adminBookingService";
import type { AdminDashboardDto } from "../../types/adminBooking";

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, { day: "2-digit", month: "short" });
}

function formatTime(time: string): string {
  return time.slice(0, 5);
}

function AdminHomePage() {
  const { user, accessToken, logout } = useAuth();
  const navigate = useNavigate();

  const [dashboard, setDashboard] = useState<AdminDashboardDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      try {
        setDashboard(await getDashboard(accessToken));
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load dashboard statistics.");
      }
    })();
  }, [accessToken]);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div>
      <h1>Administrator Application</h1>
      {user && (
        <p>
          Logged in as {user.firstName} {user.lastName} ({user.email})
        </p>
      )}
      <nav>
        <Link to="/admin/bookings">Manage Bookings</Link>
        {" | "}
        <Link to="/admin/routes">Manage Routes</Link>
        {" | "}
        <Link to="/admin/vehicles">Manage Vehicles</Link>
        {" | "}
        <Link to="/admin/drivers">Manage Drivers</Link>
      </nav>
      <button type="button" onClick={handleLogout}>
        Logout
      </button>

      {error && <p role="alert">{error}</p>}

      {dashboard && (
        <>
          <section style={{ marginTop: "1.5rem" }}>
            <h2>Overview</h2>
            <div style={{ display: "flex", gap: "1.5rem", flexWrap: "wrap" }}>
              <p>Total bookings: {dashboard.totalBookings}</p>
              <p>Today&apos;s bookings: {dashboard.todaysBookings}</p>
              <p>Pending: {dashboard.pendingBookings}</p>
              <p>Requires manual assignment: {dashboard.requiresManualAssignmentCount}</p>
              <p>Confirmed: {dashboard.confirmedBookings}</p>
              <p>Cancelled: {dashboard.cancelledBookings}</p>
              <p>Upcoming trips: {dashboard.upcomingTripsCount}</p>
            </div>
          </section>

          <section style={{ marginTop: "1.5rem" }}>
            <h2>Upcoming Bookings</h2>
            {dashboard.upcomingBookings.length === 0 ? (
              <p>No upcoming bookings.</p>
            ) : (
              <table style={{ width: "100%", borderCollapse: "collapse" }}>
                <thead>
                  <tr>
                    <th>Time</th>
                    <th>Route</th>
                    <th>Customer</th>
                    <th>Driver</th>
                    <th>Vehicle</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {dashboard.upcomingBookings.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <Link to={`/admin/bookings/${item.id}`}>
                          {formatDate(item.bookingDate)} {formatTime(item.pickupTime)}
                        </Link>
                      </td>
                      <td>
                        {item.route.departureLocation} &rarr; {item.route.destination}
                      </td>
                      <td>
                        {item.customerFirstName} {item.customerLastName}
                      </td>
                      <td>{item.driverName ?? "—"}</td>
                      <td>{item.vehicleDescription ?? "—"}</td>
                      <td>{item.status}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </section>
        </>
      )}
    </div>
  );
}

export default AdminHomePage;
