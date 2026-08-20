import { Link } from "react-router-dom";
import { APP_BRAND_NAME } from "../../config/brand";

function HomePage() {
  return (
    <div>
      <div className="container">
        <nav className="site-nav">
          <span className="site-nav__brand">{APP_BRAND_NAME}</span>
          <Link to="/booking">
            <span className="btn-secondary" style={{ display: "inline-block", padding: "0.5em 1.2em", borderRadius: "var(--radius-sm)", fontSize: "0.8rem", fontWeight: 600 }}>
              Book Now
            </span>
          </Link>
        </nav>
      </div>

      <section className="hero fade-in">
        <div className="container container--medium">
          <p className="hero__eyebrow">Private Chauffeur Service</p>
          <h1 className="hero__title">
            Your journey.
            <br />
            Our priority.
          </h1>
          <p className="hero__subtitle" style={{ marginLeft: "auto", marginRight: "auto" }}>
            Premium chauffeur transportation across Switzerland — discreet, reliable, and reserved exclusively for you.
          </p>
          <Link to="/booking">
            <button type="button" style={{ fontSize: "0.9rem", padding: "0.9em 2.2em" }}>
              Book Your Ride
            </button>
          </Link>
        </div>
      </section>

      <section className="container container--medium" style={{ paddingBottom: "var(--space-16)", textAlign: "center" }}>
        <p className="text-muted" style={{ fontSize: "0.8rem", textTransform: "uppercase", letterSpacing: "0.08em" }}>
          Executive Transportation &middot; Airport Transfers &middot; Private Events
        </p>
      </section>
    </div>
  );
}

export default HomePage;
