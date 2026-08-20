import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../../../context/AuthContext";
import { getDriverById } from "../../../services/driverService";
import { getDriverSchedule } from "../../../services/availabilityService";
import AdminNav from "../../../components/AdminNav";
import StatusBadge from "../../../components/StatusBadge";
import type { DriverDto } from "../../../types/driver";
import type { AvailabilityDto } from "../../../types/availability";

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function formatTime(time: string): string {
  return time.slice(0, 5);
}

function DriverDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { accessToken } = useAuth();

  const [driver, setDriver] = useState<DriverDto | null>(null);
  const [schedule, setSchedule] = useState<AvailabilityDto[]>([]);
  const [isCurrentlyAvailable, setIsCurrentlyAvailable] = useState(false);
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDriver = useCallback(async () => {
    if (!accessToken || !id) return;
    try {
      setDriver(await getDriverById(id, accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the driver.");
    }
  }, [accessToken, id]);

  const loadSchedule = useCallback(async () => {
    if (!accessToken || !id) return;
    setIsLoading(true);
    try {
      const result = await getDriverSchedule(id, { from: fromDate || undefined, to: toDate || undefined }, accessToken);
      setIsCurrentlyAvailable(result.isCurrentlyAvailable);
      setSchedule(result.schedule);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the driver's schedule.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, id, fromDate, toDate]);

  useEffect(() => {
    loadDriver();
  }, [loadDriver]);

  useEffect(() => {
    loadSchedule();
  }, [loadSchedule]);

  if (error)
    return (
      <div className="app-shell">
        <AdminNav />
        <main className="app-main">
          <p role="alert">{error}</p>
        </main>
      </div>
    );
  if (!driver)
    return (
      <div className="app-shell">
        <AdminNav />
        <main className="app-main">
          <div className="skeleton skeleton-line" style={{ height: 40, maxWidth: 300 }} />
        </main>
      </div>
    );

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
      <p>
        <Link to="/admin/drivers">&larr; Back to Drivers</Link>
      </p>
      <h1>
        {driver.firstName} {driver.lastName}
      </h1>

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Personal Information</h2>
        <p>Email: {driver.email}</p>
        <p>Phone: {driver.phone}</p>
      </section>

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Work Information</h2>
        <div className="row" style={{ marginBottom: "var(--space-3)" }}>
          <StatusBadge status={driver.isActive ? "Active" : "Inactive"} />
          <StatusBadge status={isCurrentlyAvailable ? "Available" : "Unavailable"} />
        </div>
        <p>Vehicle: {driver.vehicle ? `${driver.vehicle.make} ${driver.vehicle.model} - ${driver.vehicle.registrationNumber}` : "Not assigned"}</p>
      </section>

      <section>
        <h2>Availability Schedule</h2>
        <div className="row" style={{ marginBottom: "1rem" }}>
          <label>
            From:{" "}
            <input type="date" aria-label="From date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
          </label>
          <label>
            To:{" "}
            <input type="date" aria-label="To date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
          </label>
        </div>

        {isLoading ? (
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
        ) : schedule.length === 0 ? (
          <div className="empty-state">
            <p className="empty-state__title">No Availability Periods</p>
            <p>Nothing scheduled for this range.</p>
          </div>
        ) : (
          <div style={{ overflowX: "auto" }}>
          <table>
            <thead>
              <tr>
                <th>Date</th>
                <th>Start</th>
                <th>End</th>
                <th>Status</th>
                <th>Notes</th>
              </tr>
            </thead>
            <tbody>
              {schedule.map((item) => (
                <tr key={item.id}>
                  <td>{formatDate(item.date)}</td>
                  <td>{formatTime(item.startTime)}</td>
                  <td>{formatTime(item.endTime)}</td>
                  <td>
                    <StatusBadge status={item.isAvailable ? "Available" : "Unavailable"} />
                  </td>
                  <td>{item.notes}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        )}
      </section>
      </main>
    </div>
  );
}

export default DriverDetailsPage;
