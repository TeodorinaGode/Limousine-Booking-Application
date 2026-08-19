import { Link } from "react-router-dom";

function HomePage() {
  return (
    <div>
      <h1>Limousine Booking</h1>
      <p>
        <Link to="/booking">Book a Ride</Link>
      </p>
    </div>
  );
}

export default HomePage;
