import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import { getMyDashboard } from "../../services/driverBookingService";
import DriverNav from "../../components/DriverNav";
import type { DriverDashboardDto } from "../../types/driverBooking";
import TripCard from "./TripCard";

function greeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return "Good Morning";
  if (hour < 18) return "Good Afternoon";
  return "Good Evening";
}

function DriverHomePage() {
  const { user, accessToken } = useAuth();

  const [dashboard, setDashboard] = useState<DriverDashboardDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    if (!accessToken) return;
    setIsLoading(true);
    try {
      setDashboard(await getMyDashboard(accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load your dashboard.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken]);

  useEffect(() => {
    loadDashboard();
  }, [loadDashboard]);

  // The next trip already gets its own prominent section above — excluding
  // it here too avoids showing the exact same card twice on one screen.
  const remainingTodaysTrips = dashboard
    ? dashboard.todaysTrips.filter((trip) => trip.id !== dashboard.nextTrip?.id)
    : [];

  return (
    <div className="app-shell">
      <DriverNav />
      <main className="app-main app-main--narrow">
        <p className="hero__eyebrow" style={{ marginBottom: "var(--space-2)" }}>
          {greeting()}
          {user ? `, ${user.firstName.toUpperCase()}` : ""}
        </p>
        <h1>Today&apos;s Operations</h1>

        {error && <p role="alert">{error}</p>}

        {isLoading ? (
          <div className="stack" style={{ marginTop: "var(--space-6)" }}>
            <div className="skeleton skeleton-line" style={{ height: 60 }} />
            <div className="skeleton skeleton-line" style={{ height: 140 }} />
            <div className="skeleton skeleton-line" style={{ height: 140 }} />
          </div>
        ) : (
          dashboard && (
            <>
              <section style={{ marginTop: "var(--space-6)", marginBottom: "var(--space-8)" }}>
                <div className="row">
                  <div className="metric-card">
                    <div className="metric-card__label">Status</div>
                    <div className="metric-card__value" style={{ fontSize: "1.1rem" }}>
                      Currently: {dashboard.isAvailable ? "Available" : "Unavailable"}
                    </div>
                  </div>
                  <div className="metric-card">
                    <div className="metric-card__value" style={{ fontSize: "1.1rem" }}>Today&apos;s trips: {dashboard.todaysTripCount}</div>
                  </div>
                  <div className="metric-card">
                    <div className="metric-card__value" style={{ fontSize: "1.1rem" }}>Completed today: {dashboard.completedTodayCount}</div>
                  </div>
                  <div className="metric-card">
                    <div className="metric-card__value" style={{ fontSize: "1.1rem" }}>Upcoming trips: {dashboard.upcomingTripCount}</div>
                  </div>
                </div>
              </section>

              {dashboard.nextTrip && (
                <section style={{ marginBottom: "var(--space-8)" }}>
                  <h2>Next Trip</h2>
                  <TripCard trip={dashboard.nextTrip} onStatusChanged={loadDashboard} />
                </section>
              )}

              <section>
                <h2>Today&apos;s Trips</h2>
                {dashboard.todaysTrips.length === 0 ? (
                  <div className="empty-state">
                    <p className="empty-state__title">No Upcoming Trips</p>
                    <p>Your schedule is currently clear.</p>
                  </div>
                ) : remainingTodaysTrips.length === 0 ? (
                  <p className="text-muted">Your next trip above is the only one remaining today.</p>
                ) : (
                  remainingTodaysTrips.map((trip) => <TripCard key={trip.id} trip={trip} onStatusChanged={loadDashboard} />)
                )}
              </section>
            </>
          )
        )}
      </main>
    </div>
  );
}

export default DriverHomePage;
