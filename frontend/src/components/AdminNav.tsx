import { Link, useLocation, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../context/AuthContext";
import { APP_BRAND_NAME } from "../config/brand";
import LanguageSelector from "./LanguageSelector";

/** The dark sidebar shared by every admin page (section 19/54) — collapses to a horizontal bar on small screens. */
function AdminNav() {
  const location = useLocation();
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { t } = useTranslation(["admin", "common"]);

  const links = [
    { to: "/admin", label: t("nav.dashboard") },
    { to: "/admin/bookings", label: t("nav.bookings") },
    { to: "/admin/drivers", label: t("nav.drivers") },
    { to: "/admin/vehicles", label: t("nav.vehicles") },
    { to: "/admin/routes", label: t("nav.routes") },
    { to: "/admin/reports", label: t("nav.reports") },
  ];

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <nav className="app-nav" aria-label="Admin navigation">
      <div className="app-nav__brand">
        {APP_BRAND_NAME}
        <span>{t("brand.suffix")}</span>
      </div>
      <LanguageSelector />
      <div className="app-nav__links">
        {links.map((link) => (
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
          {t("common:nav.logout")}
        </button>
      </div>
    </nav>
  );
}

export default AdminNav;
