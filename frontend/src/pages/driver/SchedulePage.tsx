import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { getMyBookings } from "../../services/driverBookingService";
import type { DriverBookingListItemDto } from "../../types/driverBooking";
import TripCard from "./TripCard";

const PAGE_SIZE = 20;

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function addDaysIso(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toISOString().slice(0, 10);
}

function startOfWeekIso(): string {
  const date = new Date();
  const day = date.getDay();
  date.setDate(date.getDate() + (day === 0 ? -6 : 1 - day));
  return date.toISOString().slice(0, 10);
}

function endOfWeekIso(): string {
  const date = new Date();
  const day = date.getDay();
  date.setDate(date.getDate() + (day === 0 ? 0 : 7 - day));
  return date.toISOString().slice(0, 10);
}

function SchedulePage() {
  const { accessToken } = useAuth();

  const [trips, setTrips] = useState<DriverBookingListItemDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [dateFrom, setDateFrom] = useState(todayIso());
  const [dateTo, setDateTo] = useState(addDaysIso(7));
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadTrips = useCallback(async () => {
    if (!accessToken) return;

    setIsLoading(true);
    setError(null);
    try {
      const result = await getMyBookings(
        { dateFrom: dateFrom || undefined, dateTo: dateTo || undefined, page, pageSize: PAGE_SIZE },
        accessToken
      );
      setTrips(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load your schedule.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, dateFrom, dateTo, page]);

  useEffect(() => {
    loadTrips();
  }, [loadTrips]);

  const applyQuickDateFilter = (from: string, to: string) => {
    setPage(1);
    setDateFrom(from);
    setDateTo(to);
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div>
      <p>
        <Link to="/driver">&larr; Back to Dashboard</Link>
      </p>
      <h1>My Schedule</h1>

      <div style={{ display: "flex", gap: "0.5rem", alignItems: "center", marginBottom: "1rem", flexWrap: "wrap" }}>
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
        <button type="button" onClick={() => applyQuickDateFilter(startOfWeekIso(), endOfWeekIso())}>
          This week
        </button>
        <button type="button" onClick={() => applyQuickDateFilter(todayIso(), addDaysIso(7))}>
          Next 7 days
        </button>
        <button type="button" onClick={() => loadTrips()}>
          Refresh
        </button>
      </div>

      {error && <p role="alert">{error}</p>}

      {isLoading ? (
        <p>Loading schedule...</p>
      ) : trips.length === 0 ? (
        <p>No trips in this date range.</p>
      ) : (
        trips.map((trip) => <TripCard key={trip.id} trip={trip} />)
      )}

      {totalPages > 1 && (
        <div style={{ marginTop: "1rem" }}>
          <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
            Previous
          </button>{" "}
          <span>
            Page {page} of {totalPages}
          </span>{" "}
          <button type="button" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
            Next
          </button>
        </div>
      )}
    </div>
  );
}

export default SchedulePage;
