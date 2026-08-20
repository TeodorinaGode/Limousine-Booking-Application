import { Link } from "react-router-dom";
import StatusBadge from "../../components/StatusBadge";
import type { DriverBookingListItemDto } from "../../types/driverBooking";

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, { day: "2-digit", month: "short" });
}

function formatTime(time: string): string {
  return time.slice(0, 5);
}

/** One trip on a driver's dashboard/schedule — a reusable summary card, mobile-friendly (generous padding, a single "View trip" tap target). */
function TripCard({ trip }: { trip: DriverBookingListItemDto }) {
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
      <Link to={`/driver/bookings/${trip.id}`}>View trip &rarr;</Link>
    </div>
  );
}

export default TripCard;
