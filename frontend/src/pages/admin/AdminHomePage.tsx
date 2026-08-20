import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getDashboard } from "../../services/adminBookingService";
import { getSummary } from "../../services/reportService";
import AdminNav from "../../components/AdminNav";
import PageHeader from "../../components/PageHeader";
import StatusBadge from "../../components/StatusBadge";
import type { AdminDashboardDto } from "../../types/adminBooking";
import type { ReportSummaryDto } from "../../types/reports";

function zurichToday(): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Zurich" }).format(new Date());
}

function startOfZurichMonth(): string {
  const today = new Date(`${zurichToday()}T00:00:00`);
  return new Date(today.getFullYear(), today.getMonth(), 1).toISOString().slice(0, 10);
}

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, { day: "2-digit", month: "short" });
}

function formatTime(time: string): string {
  return time.slice(0, 5);
}

function MetricCard({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="metric-card">
      <div className="metric-card__label">{label}</div>
      <div className="metric-card__value">{value}</div>
    </div>
  );
}

function AdminHomePage() {
  const { accessToken } = useAuth();

  const [dashboard, setDashboard] = useState<AdminDashboardDto | null>(null);
  const [monthSummary, setMonthSummary] = useState<ReportSummaryDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      try {
        const [dashboardResult, summaryResult] = await Promise.all([
          getDashboard(accessToken),
          getSummary({ dateFrom: startOfZurichMonth(), dateTo: zurichToday() }, accessToken),
        ]);
        setDashboard(dashboardResult);
        setMonthSummary(summaryResult);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load dashboard statistics.");
      }
    })();
  }, [accessToken]);

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <PageHeader title="Dashboard" description="Operational overview of bookings, revenue, and today's activity." />

        {error && <p role="alert">{error}</p>}

        {dashboard && (
          <>
            <section style={{ marginBottom: "var(--space-8)" }}>
              <div className="row">
                <MetricCard label="Total Bookings" value={dashboard.totalBookings} />
                <MetricCard label="Today" value={dashboard.todaysBookings} />
                <MetricCard label="Pending" value={dashboard.pendingBookings} />
                <MetricCard label="Manual Assignment Required" value={dashboard.requiresManualAssignmentCount} />
                <MetricCard label="Confirmed" value={dashboard.confirmedBookings} />
                <MetricCard label="Cancelled" value={dashboard.cancelledBookings} />
                <MetricCard label="Upcoming Trips" value={dashboard.upcomingTripsCount} />
                {monthSummary && (
                  <div className="metric-card">
                    <div className="metric-card__value" style={{ fontSize: "1rem", fontWeight: 600 }}>
                      Completed: {monthSummary.completedBookings}
                    </div>
                  </div>
                )}
                {monthSummary && (
                  <div className="metric-card">
                    <div className="metric-card__value" style={{ fontSize: "1rem", fontWeight: 600 }}>
                      Revenue this month: {monthSummary.currency} {monthSummary.grossRevenue.toFixed(2)}
                    </div>
                  </div>
                )}
              </div>
            </section>

            <section style={{ marginBottom: "var(--space-8)" }}>
              <h2>Notifications</h2>
              <div className="row" style={{ marginTop: "var(--space-4)" }}>
                <p>Pending: {dashboard.notifications.pending}</p>
                <p>Retrying: {dashboard.notifications.retrying}</p>
                <p>Failed: {dashboard.notifications.failed}</p>
                <p>Sent today: {dashboard.notifications.sentToday}</p>
              </div>
            </section>

            <section>
              <h2>Upcoming Bookings</h2>
              {dashboard.upcomingBookings.length === 0 ? (
                <div className="empty-state">
                  <p className="empty-state__title">No Upcoming Trips</p>
                  <p>The schedule is currently clear.</p>
                </div>
              ) : (
                <div style={{ overflowX: "auto" }}>
                  <table>
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
                          <td>
                            <StatusBadge status={item.status} />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </section>
          </>
        )}
      </main>
    </div>
  );
}

export default AdminHomePage;
