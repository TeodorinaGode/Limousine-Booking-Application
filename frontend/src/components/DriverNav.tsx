import { useEffect, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../context/AuthContext";
import { getMyProfile } from "../services/driverBookingService";
import { APP_BRAND_NAME } from "../config/brand";
import LanguageSelector from "./LanguageSelector";

/** The dark navigation shared by every driver page (section 20/55), with a live availability indicator. */
function DriverNav() {
  const location = useLocation();
  const { user, accessToken, logout } = useAuth();
  const navigate = useNavigate();
  const { t } = useTranslation(["driver", "common"]);
  const [isAvailable, setIsAvailable] = useState<boolean | null>(null);

  const links = [
    { to: "/driver", label: t("nav.dashboard") },
    { to: "/driver/schedule", label: t("nav.mySchedule") },
    { to: "/driver/availability", label: t("nav.availability") },
    { to: "/driver/profile", label: t("nav.profile") },
  ];

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
        <span>{t("brand.suffix")}</span>
      </div>
      <LanguageSelector />
      {isAvailable !== null && (
        <div className={`app-nav__status ${isAvailable ? "badge--active" : "badge--cancelled"}`}>
          {isAvailable ? `● ${t("status.available")}` : `○ ${t("status.unavailable")}`}
        </div>
      )}
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

export default DriverNav;
