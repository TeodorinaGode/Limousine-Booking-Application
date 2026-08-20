import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../../context/AuthContext";
import {
  exportBookingsCsv,
  exportDriversCsv,
  exportRoutesCsv,
  exportVehiclesCsv,
  getAssignmentReport,
  getBookingsByDay,
  getCancellationReport,
  getDriverActivity,
  getPassengerReport,
  getPaymentReport,
  getPopularRoutes,
  getRevenueByDay,
  getStatusDistribution,
  getSummary,
  getUnassignedBookings,
  getUpcomingOperations,
  getVehicleUsage,
} from "../../../services/reportService";
import { getBookings } from "../../../services/adminBookingService";
import AdminNav from "../../../components/AdminNav";
import PageHeader from "../../../components/PageHeader";
import StatusBadge from "../../../components/StatusBadge";
import type {
  AssignmentReportDto,
  BookingStatusDistributionDto,
  BookingsByDayDto,
  CancellationReportDto,
  DriverActivityDto,
  PassengerReportDto,
  PaymentReportDto,
  PopularRouteDto,
  ReportSummaryDto,
  RevenueByDayDto,
  UnassignedBookingDto,
  UpcomingOperationDto,
  VehicleUsageDto,
} from "../../../types/reports";
import type { AdminBookingListItemDto } from "../../../types/adminBooking";
import MetricCard from "./MetricCard";
import DateRangeFilter from "./DateRangeFilter";
import ChartCard from "./ChartCard";
import ReportTable from "./ReportTable";
import BarChart from "./BarChart";

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

const PAGE_SIZE = 20;

function ReportsPage() {
  const { accessToken } = useAuth();

  const [dateFrom, setDateFrom] = useState(startOfZurichMonth());
  const [dateTo, setDateTo] = useState(zurichToday());
  const [routesTop, setRoutesTop] = useState<string>("10");

  const [summary, setSummary] = useState<ReportSummaryDto | null>(null);
  const [revenueByDay, setRevenueByDay] = useState<RevenueByDayDto[]>([]);
  const [bookingsByDay, setBookingsByDay] = useState<BookingsByDayDto[]>([]);
  const [routes, setRoutes] = useState<PopularRouteDto[]>([]);
  const [drivers, setDrivers] = useState<DriverActivityDto[]>([]);
  const [vehicles, setVehicles] = useState<VehicleUsageDto[]>([]);
  const [passengers, setPassengers] = useState<PassengerReportDto | null>(null);
  const [statusDistribution, setStatusDistribution] = useState<BookingStatusDistributionDto[]>([]);
  const [assignments, setAssignments] = useState<AssignmentReportDto | null>(null);
  const [cancellations, setCancellations] = useState<CancellationReportDto | null>(null);
  const [payments, setPayments] = useState<PaymentReportDto | null>(null);

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [unassigned, setUnassigned] = useState<UnassignedBookingDto[]>([]);
  const [unassignedLoading, setUnassignedLoading] = useState(true);
  const [unassignedError, setUnassignedError] = useState<string | null>(null);

  const [upcomingPeriod, setUpcomingPeriod] = useState("next7");
  const [upcoming, setUpcoming] = useState<UpcomingOperationDto[]>([]);
  const [upcomingLoading, setUpcomingLoading] = useState(true);
  const [upcomingError, setUpcomingError] = useState<string | null>(null);

  const [bookingReport, setBookingReport] = useState<AdminBookingListItemDto[]>([]);
  const [bookingReportTotal, setBookingReportTotal] = useState(0);
  const [bookingReportPage, setBookingReportPage] = useState(1);
  const [bookingReportLoading, setBookingReportLoading] = useState(true);
  const [bookingReportError, setBookingReportError] = useState<string | null>(null);

  const top = routesTop === "all" ? undefined : Number(routesTop);

  const loadReports = useCallback(async () => {
    if (!accessToken) return;
    setIsLoading(true);
    setError(null);
    try {
      const [summaryResult, revenueResult, trendResult, routesResult, driversResult, vehiclesResult, passengersResult, statusResult, assignmentsResult, cancellationsResult, paymentsResult] =
        await Promise.all([
          getSummary({ dateFrom, dateTo }, accessToken),
          getRevenueByDay({ dateFrom, dateTo }, accessToken),
          getBookingsByDay({ dateFrom, dateTo }, accessToken),
          getPopularRoutes({ dateFrom, dateTo, top }, accessToken),
          getDriverActivity({ dateFrom, dateTo }, accessToken),
          getVehicleUsage({ dateFrom, dateTo }, accessToken),
          getPassengerReport({ dateFrom, dateTo }, accessToken),
          getStatusDistribution({ dateFrom, dateTo }, accessToken),
          getAssignmentReport({ dateFrom, dateTo }, accessToken),
          getCancellationReport({ dateFrom, dateTo }, accessToken),
          getPaymentReport({ dateFrom, dateTo }, accessToken),
        ]);
      setSummary(summaryResult);
      setRevenueByDay(revenueResult);
      setBookingsByDay(trendResult);
      setRoutes(routesResult);
      setDrivers(driversResult);
      setVehicles(vehiclesResult);
      setPassengers(passengersResult);
      setStatusDistribution(statusResult);
      setAssignments(assignmentsResult);
      setCancellations(cancellationsResult);
      setPayments(paymentsResult);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load reports.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, dateFrom, dateTo, top]);

  useEffect(() => {
    loadReports();
  }, [loadReports]);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      setUnassignedLoading(true);
      setUnassignedError(null);
      try {
        setUnassigned(await getUnassignedBookings(1, 50, accessToken));
      } catch (err) {
        setUnassignedError(err instanceof Error ? err.message : "Failed to load unassigned bookings.");
      } finally {
        setUnassignedLoading(false);
      }
    })();
  }, [accessToken]);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      setUpcomingLoading(true);
      setUpcomingError(null);
      try {
        setUpcoming(await getUpcomingOperations(upcomingPeriod, accessToken));
      } catch (err) {
        setUpcomingError(err instanceof Error ? err.message : "Failed to load upcoming operations.");
      } finally {
        setUpcomingLoading(false);
      }
    })();
  }, [accessToken, upcomingPeriod]);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      setBookingReportLoading(true);
      setBookingReportError(null);
      try {
        const result = await getBookings({ dateFrom, dateTo, page: bookingReportPage, pageSize: PAGE_SIZE }, accessToken);
        setBookingReport(result.items);
        setBookingReportTotal(result.totalCount);
      } catch (err) {
        setBookingReportError(err instanceof Error ? err.message : "Failed to load the booking report.");
      } finally {
        setBookingReportLoading(false);
      }
    })();
  }, [accessToken, dateFrom, dateTo, bookingReportPage]);

  useEffect(() => {
    setBookingReportPage(1);
  }, [dateFrom, dateTo]);

  const handleDateChange = (from: string, to: string) => {
    setDateFrom(from);
    setDateTo(to);
  };

  const totalBookingsReportPages = Math.max(1, Math.ceil(bookingReportTotal / PAGE_SIZE));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
      <PageHeader title="Reports" description="Business and operational overview." />

      <DateRangeFilter dateFrom={dateFrom} dateTo={dateTo} onChange={handleDateChange} />

      {error && <p role="alert">{error}</p>}

      {isLoading ? (
        <div className="stack">
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
          <div className="skeleton skeleton-line" style={{ height: 90 }} />
        </div>
      ) : (
        summary && (
          <>
            <section style={{ marginBottom: "1.5rem" }}>
              <h2>Bookings</h2>
              <div style={{ display: "flex", gap: "1rem", flexWrap: "wrap" }}>
                <MetricCard label="Total Bookings" value={summary.totalBookings} />
                <MetricCard label="Confirmed" value={summary.confirmedBookings} />
                <MetricCard label="Pending" value={summary.pendingBookings} />
                <MetricCard label="Completed" value={summary.completedBookings} />
                <MetricCard label="Cancelled" value={summary.cancelledBookings} />
              </div>
            </section>

            <section style={{ marginBottom: "1.5rem" }}>
              <h2>Revenue</h2>
              <div style={{ display: "flex", gap: "1rem", flexWrap: "wrap" }}>
                <MetricCard label="Gross Revenue" value={`${summary.currency} ${summary.grossRevenue.toFixed(2)}`} />
                <MetricCard label="Completed Revenue" value={`${summary.currency} ${summary.completedRevenue.toFixed(2)}`} />
                <MetricCard label="Avg. Booking Value" value={`${summary.currency} ${summary.averageBookingValue.toFixed(2)}`} />
                <MetricCard label="Avg. Completed Value" value={`${summary.currency} ${summary.averageCompletedBookingValue.toFixed(2)}`} />
              </div>
            </section>

            <section style={{ marginBottom: "1.5rem" }}>
              <h2>Operations</h2>
              <div style={{ display: "flex", gap: "1rem", flexWrap: "wrap" }}>
                <MetricCard label="Automatic Assignments" value={summary.automaticAssignments} />
                <MetricCard label="Manual Assignments" value={summary.manualAssignments} />
                {assignments && <MetricCard label="Manual Assignment Rate" value={`${assignments.manualAssignmentRate}%`} />}
                {assignments && <MetricCard label="Requires Manual Assignment" value={assignments.requiresManualAssignment} />}
              </div>
            </section>

            <ChartCard title="Bookings Trend" isLoading={false} error={null} isEmpty={bookingsByDay.length === 0}>
              <BarChart
                series={[
                  { name: "Completed", color: "#f5f5f5" },
                  { name: "Cancelled", color: "#4a4a4a" },
                  { name: "Pending", color: "#8a8a8a" },
                  { name: "Confirmed", color: "#c4c4c4" },
                ]}
                data={bookingsByDay.map((d) => ({
                  label: formatDate(d.date),
                  values: [d.completed, d.cancelled, d.pending, d.confirmed],
                }))}
              />
            </ChartCard>

            <ChartCard title="Revenue Trend" isLoading={false} error={null} isEmpty={revenueByDay.length === 0}>
              <BarChart
                series={[{ name: "Revenue", color: "#d8d8d8" }]}
                data={revenueByDay.map((d) => ({ label: formatDate(d.date), values: [d.revenue] }))}
                formatValue={(v) => `${summary.currency} ${v.toFixed(2)}`}
              />
            </ChartCard>

            <ReportTable
              title="Popular Routes"
              isLoading={false}
              error={null}
              isEmpty={routes.length === 0}
              onExportCsv={() => accessToken && exportRoutesCsv({ dateFrom, dateTo, top }, accessToken)}
            >
              <>
                <thead>
                  <tr>
                    <th>Route</th>
                    <th>Bookings</th>
                    <th>Revenue</th>
                    <th>% of Total</th>
                  </tr>
                </thead>
                <tbody>
                  {routes.map((r) => (
                    <tr key={r.routeId}>
                      <td>
                        {r.departureLocation} &rarr; {r.destination}
                      </td>
                      <td>{r.bookingCount}</td>
                      <td>
                        {summary.currency} {r.revenue.toFixed(2)}
                      </td>
                      <td>{r.percentageOfTotalBookings}%</td>
                    </tr>
                  ))}
                </tbody>
              </>
            </ReportTable>
            <div style={{ marginTop: "-1rem", marginBottom: "1.5rem" }}>
              <label>
                Show:{" "}
                <select value={routesTop} onChange={(e) => setRoutesTop(e.target.value)}>
                  <option value="5">Top 5</option>
                  <option value="10">Top 10</option>
                  <option value="20">Top 20</option>
                  <option value="all">All</option>
                </select>
              </label>
            </div>

            <ReportTable
              title="Driver Activity"
              isLoading={false}
              error={null}
              isEmpty={drivers.length === 0}
              onExportCsv={() => accessToken && exportDriversCsv({ dateFrom, dateTo }, accessToken)}
            >
              <>
                <thead>
                  <tr>
                    <th>Driver</th>
                    <th>Assigned</th>
                    <th>Completed</th>
                    <th>Cancelled</th>
                    <th>Upcoming</th>
                    <th>Manual</th>
                    <th>Completion Rate</th>
                  </tr>
                </thead>
                <tbody>
                  {drivers.map((d) => (
                    <tr key={d.driverId}>
                      <td>{d.driverName}</td>
                      <td>{d.assignedBookings}</td>
                      <td>{d.completedRides}</td>
                      <td>{d.cancelledBookings}</td>
                      <td>{d.upcomingBookings}</td>
                      <td>{d.manualAssignments}</td>
                      <td>{d.completionRate}%</td>
                    </tr>
                  ))}
                </tbody>
              </>
            </ReportTable>

            <ReportTable
              title="Vehicle Usage"
              isLoading={false}
              error={null}
              isEmpty={vehicles.length === 0}
              onExportCsv={() => accessToken && exportVehiclesCsv({ dateFrom, dateTo }, accessToken)}
            >
              <>
                <thead>
                  <tr>
                    <th>Vehicle</th>
                    <th>Assigned</th>
                    <th>Completed</th>
                    <th>Upcoming</th>
                    <th>Passengers</th>
                    <th>Utilization (bookings)</th>
                  </tr>
                </thead>
                <tbody>
                  {vehicles.map((v) => (
                    <tr key={v.vehicleId}>
                      <td>{v.vehicleDescription}</td>
                      <td>{v.assignedBookings}</td>
                      <td>{v.completedRides}</td>
                      <td>{v.upcomingBookings}</td>
                      <td>{v.totalPassengers}</td>
                      <td>{v.utilization}</td>
                    </tr>
                  ))}
                </tbody>
              </>
            </ReportTable>

            {passengers && (
              <section style={{ marginBottom: "1.5rem" }}>
                <h2>Passengers</h2>
                <div style={{ display: "flex", gap: "1rem", flexWrap: "wrap" }}>
                  <MetricCard label="Total Passengers" value={passengers.totalPassengers} />
                  <MetricCard label="Average per Booking" value={passengers.averagePassengersPerBooking} />
                  <MetricCard label="Maximum in a Booking" value={passengers.maximumPassengersInABooking} />
                </div>
              </section>
            )}

            <section style={{ marginBottom: "1.5rem" }}>
              <h2>Status Distribution</h2>
              <ul>
                {statusDistribution.map((s) => (
                  <li key={s.status}>
                    {s.status}: {s.count} ({s.percentage}%)
                  </li>
                ))}
              </ul>
            </section>

            {cancellations && (
              <section style={{ marginBottom: "1.5rem" }}>
                <h2>Cancellations</h2>
                <div style={{ display: "flex", gap: "1rem", flexWrap: "wrap", marginBottom: "1rem" }}>
                  <MetricCard label="Total Cancellations" value={cancellations.totalCancellations} />
                  <MetricCard label="Cancellation Rate" value={`${cancellations.cancellationRate}%`} />
                </div>
                {cancellations.cancellationsByRoute.length > 0 && (
                  <>
                    <h3>By Route</h3>
                    <ul>
                      {cancellations.cancellationsByRoute.map((r) => (
                        <li key={r.routeId}>
                          {r.departureLocation} &rarr; {r.destination}: {r.count}
                        </li>
                      ))}
                    </ul>
                  </>
                )}
                {cancellations.cancellationsByReason.length > 0 && (
                  <>
                    <h3>By Reason</h3>
                    <ul>
                      {cancellations.cancellationsByReason.map((r) => (
                        <li key={r.reason}>
                          {r.reason}: {r.count}
                        </li>
                      ))}
                    </ul>
                  </>
                )}
              </section>
            )}

            {payments && (
              <section style={{ marginBottom: "1.5rem" }}>
                <h2>Payments</h2>
                <div style={{ display: "flex", gap: "1rem", flexWrap: "wrap", marginBottom: "1rem" }}>
                  <MetricCard label="Payment Attempts" value={payments.totalPaymentAttempts} />
                  <MetricCard label="Successful" value={payments.successfulPayments} />
                  <MetricCard label="Failed" value={payments.failedPayments} />
                  <MetricCard label="Pending" value={payments.pendingPayments} />
                  <MetricCard label="Cancelled" value={payments.cancelledPayments} />
                  <MetricCard label="Refunded" value={payments.refundedPayments} />
                </div>
                <div style={{ display: "flex", gap: "1rem", flexWrap: "wrap" }}>
                  <MetricCard label="Paid Revenue" value={`${payments.currency} ${payments.paidRevenue.toFixed(2)}`} />
                  <MetricCard label="Refunded Amount" value={`${payments.currency} ${payments.refundedAmount.toFixed(2)}`} />
                </div>
              </section>
            )}
          </>
        )
      )}

      <ReportTable
        title="Unassigned Bookings"
        isLoading={unassignedLoading}
        error={unassignedError}
        isEmpty={unassigned.length === 0}
        emptyMessage="No bookings currently require manual assignment."
      >
        <>
          <thead>
            <tr>
              <th>Booking</th>
              <th>Date</th>
              <th>Route</th>
              <th>Customer</th>
              <th>Passengers</th>
              <th>Reason</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {unassigned.map((b) => (
              <tr key={b.id}>
                <td>{b.bookingReference}</td>
                <td>
                  {formatDate(b.bookingDate)} {formatTime(b.pickupTime)}
                </td>
                <td>
                  {b.route.departureLocation} &rarr; {b.route.destination}
                </td>
                <td>
                  {b.customerFirstName} {b.customerLastName}
                </td>
                <td>{b.passengerCount}</td>
                <td>{b.reason ?? "—"}</td>
                <td>
                  <Link to={`/admin/bookings/${b.id}`}>View</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </>
      </ReportTable>

      <section style={{ marginBottom: "0.5rem" }}>
        <label>
          Upcoming operations:{" "}
          <select value={upcomingPeriod} onChange={(e) => setUpcomingPeriod(e.target.value)}>
            <option value="today">Today</option>
            <option value="next7">Next 7 days</option>
            <option value="next30">Next 30 days</option>
          </select>
        </label>
      </section>
      <ReportTable
        title="Upcoming Operations"
        isLoading={upcomingLoading}
        error={upcomingError}
        isEmpty={upcoming.length === 0}
        emptyMessage="No upcoming trips for the selected period."
      >
        <>
          <thead>
            <tr>
              <th>Booking</th>
              <th>Date</th>
              <th>Route</th>
              <th>Customer</th>
              <th>Driver</th>
              <th>Vehicle</th>
              <th>Status</th>
              <th>Ride Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {upcoming.map((b) => (
              <tr key={b.id}>
                <td>{b.bookingReference}</td>
                <td>
                  {formatDate(b.bookingDate)} {formatTime(b.pickupTime)}
                </td>
                <td>
                  {b.route.departureLocation} &rarr; {b.route.destination}
                </td>
                <td>
                  {b.customerFirstName} {b.customerLastName}
                </td>
                <td>{b.driverName ?? "—"}</td>
                <td>{b.vehicleDescription ?? "—"}</td>
                <td>
                  <StatusBadge status={b.status} />
                </td>
                <td>
                  <StatusBadge status={b.rideStatus} />
                </td>
                <td>
                  <Link to={`/admin/bookings/${b.id}`}>View</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </>
      </ReportTable>

      <ReportTable
        title="Booking Report"
        isLoading={bookingReportLoading}
        error={bookingReportError}
        isEmpty={bookingReport.length === 0}
        onExportCsv={() => accessToken && exportBookingsCsv({ dateFrom, dateTo }, accessToken)}
      >
        <>
          <thead>
            <tr>
              <th>Booking</th>
              <th>Date</th>
              <th>Route</th>
              <th>Customer</th>
              <th>Driver</th>
              <th>Vehicle</th>
              <th>Passengers</th>
              <th>Price</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {bookingReport.map((b) => (
              <tr key={b.id}>
                <td>{b.bookingReference}</td>
                <td>
                  {formatDate(b.bookingDate)} {formatTime(b.pickupTime)}
                </td>
                <td>
                  {b.route.departureLocation} &rarr; {b.route.destination}
                </td>
                <td>
                  {b.customerFirstName} {b.customerLastName}
                </td>
                <td>{b.driverName ?? "—"}</td>
                <td>{b.vehicleDescription ?? "—"}</td>
                <td>{b.passengerCount}</td>
                <td>
                  {b.currency} {b.price.toFixed(2)}
                </td>
                <td>
                  <StatusBadge status={b.status} />
                </td>
                <td>
                  <Link to={`/admin/bookings/${b.id}`}>View</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </>
      </ReportTable>
      {totalBookingsReportPages > 1 && (
        <div style={{ marginTop: "-1rem", marginBottom: "1.5rem" }}>
          <button type="button" disabled={bookingReportPage <= 1} onClick={() => setBookingReportPage((p) => p - 1)}>
            Previous
          </button>{" "}
          <span>
            Page {bookingReportPage} of {totalBookingsReportPages}
          </span>{" "}
          <button
            type="button"
            disabled={bookingReportPage >= totalBookingsReportPages}
            onClick={() => setBookingReportPage((p) => p + 1)}
          >
            Next
          </button>
        </div>
      )}
      </main>
    </div>
  );
}

export default ReportsPage;
