import { useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";
import { completeRide, markPassengerPickedUp, startRide } from "../../services/driverBookingService";
import StatusBadge from "../../components/StatusBadge";
import type { DriverBookingListItemDto } from "../../types/driverBooking";

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, { day: "2-digit", month: "short" });
}

function formatTime(time: string): string {
  return time.slice(0, 5);
}

/** The one ride-status action available for a given status, if any — same
 * transitions/endpoints TripDetailPage already used, just reachable directly
 * from the card now instead of requiring a trip into the detail page first
 * (the single biggest speed cost the prior design had: every status change
 * needed a "View trip" tap before the action button was even visible). */
const NEXT_ACTION: Partial<Record<string, { label: string; run: (id: string, accessToken: string) => Promise<unknown> }>> = {
  Upcoming: { label: "On The Way", run: startRide },
  OnTheWay: { label: "Passenger Picked Up", run: markPassengerPickedUp },
  PassengerPickedUp: { label: "Completed", run: completeRide },
};

interface TripCardProps {
  trip: DriverBookingListItemDto;
  /** Called after a status action succeeds, so the parent list/dashboard can refresh. */
  onStatusChanged?: () => void;
}

/** One trip on a driver's dashboard/schedule — mobile-friendly, generous
 * padding, and (unlike before) the next ride-status action is a large button
 * directly on the card — no detour through the detail page for the action
 * that matters most: advancing a trip while driving. */
function TripCard({ trip, onStatusChanged }: TripCardProps) {
  const { accessToken } = useAuth();
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const action = NEXT_ACTION[trip.rideStatus];

  const handleAction = async () => {
    if (!action || !accessToken || isBusy) return;
    setError(null);
    setIsBusy(true);
    try {
      await action.run(trip.id, accessToken);
      onStatusChanged?.();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update ride status.");
    } finally {
      setIsBusy(false);
    }
  };

  return (
    <div className="trip-card" style={{ marginBottom: "var(--space-3)" }}>
      <div className="row row--between" style={{ alignItems: "flex-start" }}>
        <div>
          <div style={{ fontSize: "1.75rem", fontWeight: 800, lineHeight: 1, marginBottom: "var(--space-1)" }}>
            {formatTime(trip.pickupTime)}
          </div>
          <div className="text-muted" style={{ fontSize: "0.75rem", textTransform: "uppercase", letterSpacing: "0.05em" }}>
            {formatDate(trip.bookingDate)}
          </div>
        </div>
        <StatusBadge status={trip.rideStatus} />
      </div>
      <p className="trip-card__route" style={{ marginTop: "var(--space-4)" }}>
        <span>{trip.route.departureLocation}</span>
        <span className="trip-card__arrow">&rarr;</span>
        <span>{trip.route.destination}</span>
      </p>
      <p className="trip-card__meta">Pickup: {trip.pickupAddress}</p>
      <p className="trip-card__meta">
        {trip.customerFirstName} {trip.customerLastName} &middot; {trip.passengerCount} passenger(s)
      </p>

      {error && <p role="alert" className="form-error">{error}</p>}

      <div className="trip-card__footer">
        {action && (
          <button type="button" className="trip-card__action" onClick={handleAction} disabled={isBusy}>
            {isBusy ? "Updating…" : action.label}
          </button>
        )}
        <Link to={`/driver/bookings/${trip.id}`} className="trip-card__link">View trip details &rarr;</Link>
      </div>
    </div>
  );
}

export default TripCard;
