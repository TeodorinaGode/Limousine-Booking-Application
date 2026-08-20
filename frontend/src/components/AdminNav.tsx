import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { APP_BRAND_NAME } from "../config/brand";

const LINKS = [
  { to: "/admin", label: "Dashboard" },
  { to: "/admin/bookings", label: "Bookings" },
  { to: "/admin/drivers", label: "Drivers" },
  { to: "/admin/vehicles", label: "Vehicles" },
  { to: "/admin/routes", label: "Routes" },
  { to: "/admin/reports", label: "Reports" },
];

/** The dark sidebar shared by every admin page (section 19/54) — collapses to a horizontal bar on small screens. */
function AdminNav() {
  const location = useLocation();
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <nav className="app-nav" aria-label="Admin navigation">
      <div className="app-nav__brand">
        {APP_BRAND_NAME}
        <span>Administration</span>
      </div>
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

export default AdminNav;
