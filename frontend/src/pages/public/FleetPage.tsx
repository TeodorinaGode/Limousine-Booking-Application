import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { getActiveVehicles } from "../../services/publicVehicleService";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import MobileBookingCta from "../../components/MobileBookingCta";
import { usePageMeta } from "../../hooks/usePageMeta";
import type { PublicVehicleDto } from "../../types/publicVehicle";

/** Public Fleet page (Prompt 17, section 11/12) — sourced from GET /api/public/vehicles, which already excludes inactive vehicles and internal fields (registration number, notes). Never hard-coded into React. */
function FleetPage() {
  const { t } = useTranslation("site");
  usePageMeta(`${t("fleetPage.title")} | ${t("footer.description")}`, t("fleetPage.subtitle"));

  const [vehicles, setVehicles] = useState<PublicVehicleDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      setIsLoading(true);
      setError(null);
      try {
        setVehicles(await getActiveVehicles());
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load the fleet.");
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
          <p className="section__eyebrow">{t("nav.fleet")}</p>
          <h1 className="section__title">{t("fleetPage.title")}</h1>
          <p className="section__subtitle" style={{ marginLeft: "auto", marginRight: "auto" }}>{t("fleetPage.subtitle")}</p>
        </div>
      </section>

      <section className="section" style={{ paddingTop: 0 }}>
        <div className="container">
          {error && <p role="alert">{error}</p>}
          {isLoading ? (
            <div className="stack">
              <div className="skeleton skeleton-line" style={{ height: 90 }} />
              <div className="skeleton skeleton-line" style={{ height: 90 }} />
            </div>
          ) : vehicles.length === 0 ? (
            <div className="empty-state">
              <p className="empty-state__title">{t("fleetPage.noVehicles")}</p>
            </div>
          ) : (
            <div className="grid grid--3">
              {vehicles.map((vehicle) => (
                <article className="content-card" key={vehicle.id}>
                  <div className="content-card__media" role="img" aria-label={`${vehicle.make} ${vehicle.model}`}>🚘</div>
                  <h2 className="content-card__title" style={{ textTransform: "none" }}>{vehicle.make} {vehicle.model}</h2>
                  <p className="content-card__desc">{vehicle.vehicleType}</p>
                  <p className="content-card__meta">
                    <span>{t("fleetPage.passengers")}: {vehicle.passengerCapacity}</span>
                  </p>
                  <Link to="/booking" className="btn-secondary" style={{ textAlign: "center", padding: "0.6em", borderRadius: "var(--radius-sm)" }}>
                    {t("fleetTeaser.bookYourJourney")}
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

export default FleetPage;
