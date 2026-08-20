import { useEffect, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { getMyProfile } from "../services/driverBookingService";
import { APP_BRAND_NAME } from "../config/brand";

const LINKS = [
  { to: "/driver", label: "Dashboard" },
  { to: "/driver/schedule", label: "My Schedule" },
  { to: "/driver/availability", label: "Availability" },
  { to: "/driver/profile", label: "Profile" },
];

/** The dark navigation shared by every driver page (section 20/55), with a live availability indicator. */
function DriverNav() {
  const location = useLocation();
  const { user, accessToken, logout } = useAuth();
  const navigate = useNavigate();
  const [isAvailable, setIsAvailable] = useState<boolean | null>(null);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      try {
        const profile = await getMyProfile(accessToken);
        setIsAvailable(profile.isAvailable);
      } catch {
        // The status indicator is a convenience — a failed fetch just hides it.
      }
    })();
  }, [accessToken, location.pathname]);

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <nav className="app-nav" aria-label="Driver navigation">
      <div className="app-nav__brand">
        {APP_BRAND_NAME}
        <span>Driver</span>
      </div>
      {isAvailable !== null && (
        <div className={`app-nav__status ${isAvailable ? "badge--active" : "badge--cancelled"}`}>
          {isAvailable ? "● Available" : "○ Unavailable"}
        </div>
      )}
      <div className="app-nav__links">
        {LINKS.map((link) => (
          <Link
            key={link.to}
            to={link.to}
            className={`app-nav__link${location.pathname === link.to ? " app-nav__link--active" : ""}`}
          >
            {link.label}
          </Link>
        ))}
      </div>
      <div className="stack">
        {user && <span className="text-muted" style={{ fontSize: "0.75rem" }}>{user.firstName} {user.lastName}</span>}
        <button type="button" className="btn-secondary" onClick={handleLogout}>
          Logout
        </button>
      </div>
    </nav>
  );
}

export default DriverNav;
