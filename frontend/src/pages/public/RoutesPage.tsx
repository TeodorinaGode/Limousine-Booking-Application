import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { getActiveRoutes } from "../../services/bookingService";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import MobileBookingCta from "../../components/MobileBookingCta";
import { usePageMeta } from "../../hooks/usePageMeta";
import type { PublicRouteDto } from "../../types/booking";

/** Public Routes/Destinations page (Prompt 17, section 9) — lists only active routes (the public API already excludes inactive ones, section 9/40), with real duration/price from the backend, never hard-coded (section 8). */
function RoutesPage() {
  const { t } = useTranslation("site");
  usePageMeta(`${t("routesPage.title")} | ${t("footer.description")}`, t("routesPage.subtitle"));

  const [routes, setRoutes] = useState<PublicRouteDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      setIsLoading(true);
      setError(null);
      try {
        setRoutes(await getActiveRoutes());
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load routes.");
      } finally {
        setIsLoading(false);
      }
    })();
  }, []);

  return (
    <div>
      <Header />

      <section className="section section--center">
        <div className="container">
          <p className="section__eyebrow">{t("nav.routes")}</p>
          <h1 className="section__title">{t("routesPage.title")}</h1>
          <p className="section__subtitle" style={{ marginLeft: "auto", marginRight: "auto" }}>{t("routesPage.subtitle")}</p>
        </div>
      </section>

      <section className="section" style={{ paddingTop: 0 }}>
        <div className="container">
          {error && <p role="alert">{error}</p>}
          {isLoading ? (
            <div className="stack">
              <div className="skeleton skeleton-line" style={{ height: 90 }} />
              <div className="skeleton skeleton-line" style={{ height: 90 }} />
              <div className="skeleton skeleton-line" style={{ height: 90 }} />
            </div>
          ) : routes.length === 0 ? (
            <div className="empty-state">
              <p className="empty-state__title">{t("routesPage.noRoutes")}</p>
            </div>
          ) : (
            <div className="grid grid--3">
              {routes.map((route) => (
                <article className="content-card" key={route.id}>
                  <div className="trip-card__route" style={{ marginBottom: 0 }}>
                    <span>{route.departureLocation}</span>
                    <span className="trip-card__arrow">&rarr;</span>
                    <span>{route.destination}</span>
                  </div>
                  <p className="content-card__meta">
                    <span>{t("routesTeaser.duration")}: {Math.floor(route.estimatedDurationMinutes / 60)}h {route.estimatedDurationMinutes % 60}m</span>
                  </p>
                  <p className="trip-card__price" style={{ marginTop: 0 }}>
                    {t("routesTeaser.from")} {route.price.toFixed(2)} {route.currency}
                  </p>
                  <Link
                    to="/booking"
                    state={{ routeId: route.id }}
                    className="btn-secondary"
                    style={{ textAlign: "center", padding: "0.6em", borderRadius: "var(--radius-sm)" }}
                  >
                    {t("routesTeaser.bookThisRoute")}
                  </Link>
                </article>
              ))}
            </div>
          )}
        </div>
      </section>

      <Footer />
      <MobileBookingCta />
    </div>
  );
}

export default RoutesPage;
