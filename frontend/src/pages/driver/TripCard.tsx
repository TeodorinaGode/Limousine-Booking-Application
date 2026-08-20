import { Link } from "react-router-dom";
import type { DriverBookingListItemDto } from "../../types/driverBooking";

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, { day: "2-digit", month: "short" });
}

function formatTime(time: string): string {
  return time.slice(0, 5);
}

const cardStyle: React.CSSProperties = {
  border: "1px solid #ccc",
  borderRadius: "8px",
  padding: "1rem",
  marginBottom: "0.75rem",
};

/** One trip on a driver's dashboard/schedule — a reusable summary card, mobile-friendly (generous padding, a single "View trip" tap target). */
function TripCard({ trip }: { trip: DriverBookingListItemDto }) {
  return (
    <div style={cardStyle}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline", flexWrap: "wrap", gap: "0.5rem" }}>
        <strong>
          {formatDate(trip.bookingDate)} at {formatTime(trip.pickupTime)}
        </strong>
        <span>{trip.rideStatus}</span>
      </div>
      <p style={{ margin: "0.5rem 0" }}>
        {trip.route.departureLocation} &rarr; {trip.route.destination}
      </p>
      <p style={{ margin: "0.25rem 0" }}>
        {trip.customerFirstName} {trip.customerLastName} &middot; {trip.passengerCount} passenger(s)
      </p>
      <p style={{ margin: "0.25rem 0", color: "#555" }}>{trip.pickupAddress}</p>
      <Link to={`/driver/bookings/${trip.id}`}>View trip &rarr;</Link>
    </div>
  );
}

export default TripCard;
