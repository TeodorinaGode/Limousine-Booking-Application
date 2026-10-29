import { Link, useLocation, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../context/AuthContext";
import useSidebarCollapse from "../hooks/useSidebarCollapse";
import { APP_BRAND_NAME } from "../config/brand";
import LanguageSelector from "./LanguageSelector";
import NotificationBell from "./NotificationBell";

/** The dark sidebar shared by every admin page (section 19/54) — collapses to a horizontal bar on small screens, and can also be manually collapsed to an icon rail via the toggle button. */
function AdminNav() {
  const location = useLocation();
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { t } = useTranslation(["admin", "common"]);
  const [isCollapsed, toggleCollapsed] = useSidebarCollapse();

  const links = [
    { to: "/admin/crm", label: t("nav.crm"), icon: "🤝" },
    { to: "/admin/dashboard", label: t("nav.dashboard"), icon: "📊" },
    { to: "/admin/bookings", label: t("nav.bookings"), icon: "📅" },
    { to: "/admin/drivers", label: t("nav.drivers"), icon: "👤" },
    { to: "/admin/vehicles", label: t("nav.vehicles"), icon: "🚗" },
    { to: "/admin/routes", label: t("nav.routes"), icon: "🗺️" },
    { to: "/admin/reports", label: t("nav.reports"), icon: "📈" },
  ];

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <nav className={`app-nav${isCollapsed ? " app-nav--collapsed" : ""}`} aria-label="Admin navigation">
      <div className="app-nav__header">
        {!isCollapsed && (
          <div className="app-nav__brand">
            {APP_BRAND_NAME}
            <span>{t("brand.suffix")}</span>
          </div>
        )}
        <button
          type="button"
          className="app-nav__toggle"
          onClick={toggleCollapsed}
          aria-label={isCollapsed ? "Expand menu" : "Collapse menu"}
          title={isCollapsed ? "Expand menu" : "Collapse menu"}
        >
          {isCollapsed ? "»" : "«"}
        </button>
      </div>
      <NotificationBell isCollapsed={isCollapsed} />
      {!isCollapsed && <LanguageSelector />}
      <div className="app-nav__links">
        {links.map((link) => (
          <Link
            key={link.to}
            to={link.to}
            className={`app-nav__link${location.pathname === link.to ? " app-nav__link--active" : ""}`}
            title={isCollapsed ? link.label : undefined}
          >
            <span aria-hidden="true" className="app-nav__link-icon">{link.icon}</span>
            {!isCollapsed && link.label}
          </Link>
        ))}
      </div>
      <div className="stack">
        {!isCollapsed && user && <span className="text-muted" style={{ fontSize: "0.75rem" }}>{user.firstName} {user.lastName}</span>}
        <button type="button" className="btn-secondary" onClick={handleLogout} title={isCollapsed ? t("common:nav.logout") : undefined}>
          {isCollapsed ? "⏻" : t("common:nav.logout")}
        </button>
      </div>
    </nav>
  );
}

export default AdminNav;
