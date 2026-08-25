import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { getActiveVehicles } from "../../services/publicVehicleService";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import MobileBookingCta from "../../components/MobileBookingCta";
import { usePageMeta } from "../../hooks/usePageMeta";
import { useScrollReveal } from "../../hooks/useScrollReveal";
import type { PublicVehicleDto } from "../../types/publicVehicle";

/**
 * One full-width cinematic reveal per vehicle (Prompt 21, section 16/17) —
 * its own component (not inlined in a `.map()`) specifically so each
 * instance gets its own stable `useScrollReveal()` call; calling a hook
 * inside a loop directly would violate the rules of hooks once the vehicle
 * list's length changes between renders (0 while loading, then N).
 */
function FleetVehicleReveal({ vehicle }: { vehicle: PublicVehicleDto }) {
  const { t } = useTranslation("site");
  const reveal = useScrollReveal<HTMLDivElement>();

  return (
    <div ref={reveal.ref} className={`fleet-cinematic ${reveal.className}`} style={{ marginBottom: "var(--space-8)" }}>
      <div>
        <p className="fleet-cinematic__label">{vehicle.vehicleType}</p>
        <h2 className="fleet-cinematic__name">
          <span style={{ display: "block" }}>{vehicle.make}</span>
          <span style={{ display: "block" }}>{vehicle.model}</span>
        </h2>
        <p className="text-secondary" style={{ marginTop: "var(--space-4)" }}>
          {t("fleetPage.passengers")}: {vehicle.passengerCapacity}
        </p>
        <Link to="/booking" className="cta-noir cta-noir--solid" style={{ marginTop: "var(--space-4)", display: "inline-flex" }}>
          {t("fleetTeaser.bookYourJourney")}<span aria-hidden="true"> &rarr;</span>
        </Link>
      </div>
    </div>
  );
}

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
            vehicles.map((vehicle) => <FleetVehicleReveal key={vehicle.id} vehicle={vehicle} />)
          )}
        </div>
      </section>

      <Footer />
      <MobileBookingCta />
    </div>
  );
}

export default FleetPage;
