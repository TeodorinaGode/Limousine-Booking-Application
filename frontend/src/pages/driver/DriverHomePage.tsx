import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getMyDashboard } from "../../services/driverBookingService";
import type { DriverDashboardDto } from "../../types/driverBooking";
import TripCard from "./TripCard";

function DriverHomePage() {
  const { user, accessToken, logout } = useAuth();
  const navigate = useNavigate();

  const [dashboard, setDashboard] = useState<DriverDashboardDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      try {
        setDashboard(await getMyDashboard(accessToken));
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load your dashboard.");
      }
    })();
  }, [accessToken]);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <div>
      <h1>Driver Dashboard</h1>
      {user && (
        <p>
          Logged in as {user.firstName} {user.lastName} ({user.email})
        </p>
      )}
      <nav>
        <Link to="/driver/schedule">My Schedule</Link>
        {" | "}
        <Link to="/driver/availability">My Availability</Link>
        {" | "}
        <Link to="/driver/profile">My Profile</Link>
      </nav>
      <button type="button" onClick={handleLogout}>
        Logout
      </button>

      {error && <p role="alert">{error}</p>}

      {dashboard && (
        <>
          <section style={{ marginTop: "1.5rem" }}>
            <h2>Today</h2>
            <div style={{ display: "flex", gap: "1.5rem", flexWrap: "wrap" }}>
              <p>Currently: {dashboard.isAvailable ? "Available" : "Unavailable"}</p>
              <p>Today&apos;s trips: {dashboard.todaysTripCount}</p>
              <p>Completed today: {dashboard.completedTodayCount}</p>
              <p>Upcoming trips: {dashboard.upcomingTripCount}</p>
            </div>
          </section>

          {dashboard.nextTrip && (
            <section style={{ marginTop: "1.5rem" }}>
              <h2>Next Trip</h2>
              <TripCard trip={dashboard.nextTrip} />
            </section>
          )}

          <section style={{ marginTop: "1.5rem" }}>
            <h2>Today&apos;s Trips</h2>
            {dashboard.todaysTrips.length === 0 ? (
              <p>No trips scheduled for today.</p>
            ) : (
              dashboard.todaysTrips.map((trip) => <TripCard key={trip.id} trip={trip} />)
            )}
          </section>
        </>
      )}
    </div>
  );
}

export default DriverHomePage;
