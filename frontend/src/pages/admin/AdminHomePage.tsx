import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getDashboard } from "../../services/adminBookingService";
import { getSummary, getBookingsByDay } from "../../services/reportService";
import { getDrivers } from "../../services/driverService";
import { getVehicles } from "../../services/vehicleService";
import AdminNav from "../../components/AdminNav";
import PageHeader from "../../components/PageHeader";
import StatusBadge from "../../components/StatusBadge";
import MetricCard from "./reports/MetricCard";
import ChartCard from "./reports/ChartCard";
import BarChart from "./reports/BarChart";
import type { AdminDashboardDto } from "../../types/adminBooking";
import type { ReportSummaryDto, BookingsByDayDto } from "../../types/reports";

function zurichToday(): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Zurich" }).format(new Date());
}

function startOfZurichMonth(): string {
  const today = new Date(`${zurichToday()}T00:00:00`);
  return new Date(today.getFullYear(), today.getMonth(), 1).toISOString().slice(0, 10);
}

function daysAgoIso(days: number): string {
  const date = new Date(`${zurichToday()}T00:00:00`);
  date.setDate(date.getDate() - days);
  return date.toISOString().slice(0, 10);
}

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, { day: "2-digit", month: "short" });
}

function formatTime(time: string): string {
  return time.slice(0, 5);
}

interface DashboardData {
  dashboard: AdminDashboardDto;
  monthSummary: ReportSummaryDto;
  availableDriversCount: number;
  activeVehiclesCount: number;
  bookingsTrend: BookingsByDayDto[];
}

function AdminHomePage() {
  const { accessToken } = useAuth();

  const [data, setData] = useState<DashboardData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      setIsLoading(true);
      setError(null);
      try {
        const [dashboard, monthSummary, availableDrivers, activeVehicles, bookingsTrend] = await Promise.all([
          getDashboard(accessToken),
          getSummary({ dateFrom: startOfZurichMonth(), dateTo: zurichToday() }, accessToken),
          getDrivers({ isActive: true, isAvailable: true, page: 1, pageSize: 1 }, accessToken),
          getVehicles({ isActive: true, page: 1, pageSize: 1 }, accessToken),
          getBookingsByDay({ dateFrom: daysAgoIso(6), dateTo: zurichToday() }, accessToken),
        ]);
        setData({
          dashboard,
          monthSummary,
          availableDriversCount: availableDrivers.totalCount,
          activeVehiclesCount: activeVehicles.totalCount,
          bookingsTrend,
        });
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load dashboard statistics.");
      } finally {
        setIsLoading(false);
      }
    })();
  }, [accessToken]);

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <PageHeader title="Dashboard" description="Operational overview of bookings, revenue, and today's activity." />

        {error && <p role="alert">{error}</p>}

        {isLoading ? (
          <div className="stack" style={{ marginTop: "var(--space-2)" }}>
            <div className="skeleton skeleton-line" style={{ height: 90 }} />
            <div className="skeleton skeleton-line" style={{ height: 220 }} />
            <div className="skeleton skeleton-line" style={{ height: 160 }} />
          </div>
        ) : (
          data && (
            <>
              {/* Headline numbers — the four figures an admin most needs at a glance. */}
              <section style={{ marginBottom: "var(--space-6)" }}>
                <div className="row">
                  <MetricCard label="Total Bookings" value={data.dashboard.totalBookings} />
                  <MetricCard label="Today's Bookings" value={data.dashboard.todaysBookings} />
                  <MetricCard label="Upcoming Trips" value={data.dashboard.upcomingTripsCount} />
                  <MetricCard label="Revenue This Month" value={`${data.monthSummary.currency} ${data.monthSummary.grossRevenue.toFixed(2)}`} />
                </div>
              </section>

              {/* Resource availability — can today's demand actually be served. */}
              <section style={{ marginBottom: "var(--space-6)" }}>
                <div className="row">
                  <MetricCard label="Available Drivers" value={data.availableDriversCount} />
                  <MetricCard label="Active Vehicles" value={data.activeVehiclesCount} />
                  <MetricCard label="Completed This Month" value={data.monthSummary.completedBookings} />
                </div>
              </section>

              {/* Booking-status breakdown. */}
              <section style={{ marginBottom: "var(--space-8)" }}>
                <div className="row">
                  <MetricCard label="Pending" value={data.dashboard.pendingBookings} />
                  <MetricCard label="Confirmed" value={data.dashboard.confirmedBookings} />
                  <MetricCard label="Cancelled" value={data.dashboard.cancelledBookings} />
                  <MetricCard label="Manual Assignment Required" value={data.dashboard.requiresManualAssignmentCount} />
                </div>
              </section>

              <ChartCard
                title="Bookings — Last 7 Days"
                isLoading={false}
                error={null}
                isEmpty={data.bookingsTrend.length === 0}
                emptyMessage="No bookings in the last 7 days."
              >
                <BarChart
                  series={[{ name: "Bookings", color: "var(--color-accent-strong)" }]}
                  data={data.bookingsTrend.map((day) => ({ label: formatDate(day.date), values: [day.total] }))}
                  height={180}
                />
              </ChartCard>

              <section style={{ marginBottom: "var(--space-8)" }}>
                <h2>Notifications</h2>
                <div className="row" style={{ marginTop: "var(--space-4)" }}>
                  <p>Pending: {data.dashboard.notifications.pending}</p>
                  <p>Retrying: {data.dashboard.notifications.retrying}</p>
                  <p>Failed: {data.dashboard.notifications.failed}</p>
                  <p>Sent today: {data.dashboard.notifications.sentToday}</p>
                </div>
              </section>

              <section>
                <h2>Upcoming Bookings</h2>
                {data.dashboard.upcomingBookings.length === 0 ? (
                  <div className="empty-state">
                    <p className="empty-state__title">No Upcoming Trips</p>
                    <p>The schedule is currently clear.</p>
                  </div>
                ) : (
                  <div className="table-responsive" style={{ overflowX: "auto" }}>
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
                        {data.dashboard.upcomingBookings.map((item) => (
                          <tr key={item.id}>
                            <td data-label="Time">
                              <Link to={`/admin/bookings/${item.id}`}>
                                {formatDate(item.bookingDate)} {formatTime(item.pickupTime)}
                              </Link>
                            </td>
                            <td data-label="Route">
                              {item.route.departureLocation} &rarr; {item.route.destination}
                            </td>
                            <td data-label="Customer">
                              {item.customerFirstName} {item.customerLastName}
                            </td>
                            <td data-label="Driver">{item.driverName ?? "—"}</td>
                            <td data-label="Vehicle">{item.vehicleDescription ?? "—"}</td>
                            <td data-label="Status">
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
          )
        )}
      </main>
    </div>
  );
}

export default AdminHomePage;
