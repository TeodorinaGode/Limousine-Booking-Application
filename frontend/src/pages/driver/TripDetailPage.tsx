import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { completeRide, getMyBookingById, markPassengerPickedUp, startRide } from "../../services/driverBookingService";
import DriverNav from "../../components/DriverNav";
import StatusBadge from "../../components/StatusBadge";
import type { DriverBookingDetailDto } from "../../types/driverBooking";

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, { day: "2-digit", month: "long", year: "numeric" });
}

function formatTime(time: string): string {
  return time.slice(0, 5);
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString();
}

function TripDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { accessToken } = useAuth();

  const [trip, setTrip] = useState<DriverBookingDetailDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isBusy, setIsBusy] = useState(false);

  const loadTrip = useCallback(async () => {
    if (!accessToken || !id) return;
    setIsLoading(true);
    setError(null);
    try {
      setTrip(await getMyBookingById(id, accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the trip.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, id]);

  useEffect(() => {
    loadTrip();
  }, [loadTrip]);

  useEffect(() => {
    if (!successMessage) return;
    const timeout = setTimeout(() => setSuccessMessage(null), 3000);
    return () => clearTimeout(timeout);
  }, [successMessage]);

  const runAction = async (action: (id: string, accessToken: string) => Promise<DriverBookingDetailDto>, message: string) => {
    if (!accessToken || !id) return;
    setError(null);
    setSuccessMessage(null);
    setIsBusy(true);
    try {
      setTrip(await action(id, accessToken));
      setSuccessMessage(message);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the ride status.");
    } finally {
      setIsBusy(false);
    }
  };

  if (error && !trip)
    return (
      <div className="app-shell">
        <DriverNav />
        <main className="app-main app-main--narrow">
          <p role="alert">{error}</p>
        </main>
      </div>
    );
  if (isLoading || !trip)
    return (
      <div className="app-shell">
        <DriverNav />
        <main className="app-main app-main--narrow">
          <div className="skeleton skeleton-line" style={{ height: 40, maxWidth: 300 }} />
        </main>
      </div>
    );

  return (
    <div className="app-shell">
      <DriverNav />
      <main className="app-main app-main--narrow">
      <p>
        <Link to="/driver/schedule">&larr; Back to Schedule</Link>
      </p>

      {successMessage && <p role="status">{successMessage}</p>}
      {error && <p role="alert">{error}</p>}

      <section style={{ marginBottom: "1.5rem" }}>
        <h1>Trip {trip.bookingReference}</h1>
        <div className="row">
          <StatusBadge status={trip.status} />
          <StatusBadge status={trip.rideStatus} />
        </div>
      </section>

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Passenger</h2>
        <p>
          {trip.customerFirstName} {trip.customerLastName}
          <br />
          {trip.customerPhone}
        </p>
      </section>

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Trip</h2>
        <p>
          {trip.route.departureLocation} &rarr; {trip.route.destination}
        </p>
        <p>Date: {formatDate(trip.bookingDate)}</p>
        <p>Pickup: {formatTime(trip.pickupTime)}</p>
        <p>Estimated arrival: {formatTime(trip.estimatedEndTime)}</p>
        <p>Pickup address: {trip.pickupAddress}</p>
        <p>Passengers: {trip.passengerCount}</p>
      </section>

      {trip.notes && (
        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Notes</h2>
          <p>{trip.notes}</p>
        </section>
      )}

      <div className="stack" style={{ marginBottom: "1.5rem" }}>
        {trip.rideStatus === "Upcoming" && (
          <button type="button" className="btn-block" onClick={() => runAction(startRide, "Ride started.")} disabled={isBusy}>
            Start Ride
          </button>
        )}
        {trip.rideStatus === "OnTheWay" && (
          <button
            type="button"
            className="btn-block"
            onClick={() => runAction(markPassengerPickedUp, "Passenger marked as picked up.")}
            disabled={isBusy}
          >
            Passenger Picked Up
          </button>
        )}
        {trip.rideStatus === "PassengerPickedUp" && (
          <button type="button" className="btn-block" onClick={() => runAction(completeRide, "Ride completed.")} disabled={isBusy}>
            Complete Ride
          </button>
        )}
      </div>

      {trip.rideStatusHistory.length > 0 && (
        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Ride Status History</h2>
          <div style={{ overflowX: "auto" }}>
          <table>
            <thead>
              <tr>
                <th>From</th>
                <th>To</th>
                <th>Changed At</th>
              </tr>
            </thead>
            <tbody>
              {trip.rideStatusHistory.map((entry, index) => (
                <tr key={index}>
                  <td>{entry.previousStatus}</td>
                  <td>{entry.newStatus}</td>
                  <td>{formatDateTime(entry.changedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        </section>
      )}
      </main>
    </div>
  );
}

export default TripDetailPage;
