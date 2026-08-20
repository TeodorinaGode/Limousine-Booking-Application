import { lazy, Suspense, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { getActiveRoutes } from "../../services/bookingService";
import { getActiveVehicles } from "../../services/publicVehicleService";
import { getLocations } from "../../services/locationService";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import MobileBookingCta from "../../components/MobileBookingCta";
import HeroBookingWidget from "../../components/HeroBookingWidget";
import { usePageMeta } from "../../hooks/usePageMeta";
import { useCompanyInfo } from "../../hooks/useCompanyInfo";
import { SERVICES } from "../../config/services";
import type { PublicRouteDto } from "../../types/booking";
import type { PublicVehicleDto } from "../../types/publicVehicle";
import type { PublicLocationsDto } from "../../types/location";

const ServiceAreaMap = lazy(() => import("../../components/ServiceAreaMap"));

const TRUST_ITEMS = [
  { key: "chauffeur", icon: "🎩" },
  { key: "onTime", icon: "⏱" },
  { key: "vehicles", icon: "✦" },
  { key: "available", icon: "☎" },
] as const;

const WHY_US_ITEMS = ["reliability", "luxury", "professionalChauffeurs"] as const;

function HomePage() {
  const { t } = useTranslation(["site", "booking"]);
  usePageMeta(t("seo.homeTitle"), t("seo.homeDescription"));
  const company = useCompanyInfo();

  const [routes, setRoutes] = useState<PublicRouteDto[]>([]);
  const [vehicles, setVehicles] = useState<PublicVehicleDto[]>([]);
  const [locationsData, setLocationsData] = useState<PublicLocationsDto | null>(null);

  useEffect(() => {
    getActiveRoutes().then(setRoutes).catch(() => undefined);
    getActiveVehicles().then(setVehicles).catch(() => undefined);
    getLocations().then(setLocationsData).catch(() => undefined);
  }, []);

  return (
    <div>
      <Header />

      <section className="hero fade-in">
        <div className="container container--medium">
          <p className="hero__eyebrow">{t("hero.eyebrow")}</p>
          <h1 className="hero__title">
            {t("hero.title1")}
            <br />
            {t("hero.title2")}
          </h1>
          <p className="hero__subtitle" style={{ marginLeft: "auto", marginRight: "auto" }}>{t("hero.subtitle")}</p>
          <div className="row" style={{ justifyContent: "center" }}>
            <Link to="/booking"><button type="button" style={{ fontSize: "0.9rem", padding: "0.9em 2.2em" }}>{t("hero.ctaPrimary")}</button></Link>
            <Link to="/services"><button type="button" className="btn-secondary" style={{ fontSize: "0.9rem", padding: "0.9em 2.2em" }}>{t("hero.ctaSecondary")}</button></Link>
          </div>
        </div>
      </section>

      <div className="container" style={{ marginTop: "calc(-1 * var(--space-8))", marginBottom: "var(--space-4)" }}>
        <HeroBookingWidget routes={routes} />
      </div>

      <section className="section section--tight">
        <div className="container grid grid--4">
          {TRUST_ITEMS.map((item) => (
            <div className="trust-item" key={item.key}>
              <div className="trust-item__icon" role="img" aria-label={t(`trust.${item.key}.title`)}>{item.icon}</div>
              <p className="trust-item__title">{t(`trust.${item.key}.title`)}</p>
              <p className="trust-item__desc">{t(`trust.${item.key}.desc`)}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="section section--elevated">
        <div className="container section--center">
          <p className="section__eyebrow">{t("nav.services")}</p>
          <h2 className="section__title">{t("services.title")}</h2>
          <p className="section__subtitle">{t("services.subtitle")}</p>
        </div>
        <div className="container grid grid--3">
          {SERVICES.map((service) => (
            <article className="content-card" key={service.key}>
              <div className="content-card__media" role="img" aria-label={t(`services.${service.key}.title`)}>{service.icon}</div>
              <h3 className="content-card__title">{t(`services.${service.key}.title`)}</h3>
              <p className="content-card__desc">{t(`services.${service.key}.desc`)}</p>
              <Link to="/booking" className="btn-secondary" style={{ textAlign: "center", padding: "0.6em", borderRadius: "var(--radius-sm)" }}>
                {t("services.bookTransfer")}
              </Link>
            </article>
          ))}
        </div>
      </section>

      <section className="section">
        <div className="container">
          <div className="row row--between" style={{ alignItems: "flex-end", marginBottom: "var(--space-8)" }}>
            <div>
              <p className="section__eyebrow">{t("nav.routes")}</p>
              <h2 className="section__title" style={{ marginBottom: 0 }}>{t("routesTeaser.title")}</h2>
            </div>
            <Link to="/routes">{t("routesTeaser.viewAll")}</Link>
          </div>
          {routes.length === 0 ? (
            <div className="stack">
              <div className="skeleton skeleton-line" style={{ height: 90 }} />
              <div className="skeleton skeleton-line" style={{ height: 90 }} />
            </div>
          ) : (
            <div className="grid grid--3">
              {routes.slice(0, 3).map((route) => (
                <article className="content-card" key={route.id}>
                  <div className="trip-card__route" style={{ marginBottom: 0 }}>
                    <span>{route.departureLocation}</span>
                    <span className="trip-card__arrow">&rarr;</span>
                    <span>{route.destination}</span>
                  </div>
                  <p className="content-card__meta">
                    <span>{t("routesTeaser.duration")}: {Math.floor(route.estimatedDurationMinutes / 60)}h {route.estimatedDurationMinutes % 60}m</span>
                    <span>{t("routesTeaser.from")} {route.price.toFixed(0)} {route.currency}</span>
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

      {locationsData && locationsData.enabled && locationsData.locations.length > 0 && (
        <section className="section">
          <div className="container section--center">
            <p className="section__eyebrow">{t("serviceAreaMap.eyebrow")}</p>
            <h2 className="section__title">{t("serviceAreaMap.title")}</h2>
            <p className="section__subtitle">{t("serviceAreaMap.subtitle")}</p>
          </div>
          <div className="container">
            <Suspense fallback={<div className="service-area-map__skeleton" />}>
              <ServiceAreaMap
                locations={locationsData.locations}
                routes={routes}
                defaultLatitude={locationsData.defaultLatitude}
                defaultLongitude={locationsData.defaultLongitude}
                defaultZoom={locationsData.defaultZoom}
                height={380}
              />
            </Suspense>
          </div>
        </section>
      )}

      <section className="section section--elevated">
        <div className="container">
          <div className="row row--between" style={{ alignItems: "flex-end", marginBottom: "var(--space-8)" }}>
            <div>
              <p className="section__eyebrow">{t("nav.fleet")}</p>
              <h2 className="section__title" style={{ marginBottom: 0 }}>{t("fleetTeaser.title")}</h2>
            </div>
            <Link to="/fleet">{t("routesTeaser.viewAll")}</Link>
          </div>
          {vehicles.length > 0 && (
            <div className="grid grid--3">
              {vehicles.slice(0, 3).map((vehicle) => (
                <article className="content-card" key={vehicle.id}>
                  <div className="content-card__media" role="img" aria-label={`${vehicle.make} ${vehicle.model}`}>🚘</div>
                  <h3 className="content-card__title">{vehicle.make} {vehicle.model}</h3>
                  <p className="content-card__desc">{vehicle.vehicleType}</p>
                  <p className="content-card__meta"><span>{t("fleetPage.passengers")}: {vehicle.passengerCapacity}</span></p>
                  <Link to="/booking" className="btn-secondary" style={{ textAlign: "center", padding: "0.6em", borderRadius: "var(--radius-sm)" }}>
                    {t("fleetTeaser.bookYourJourney")}
                  </Link>
                </article>
              ))}
            </div>
          )}
        </div>
      </section>

      <section className="section">
        <div className="container section--center">
          <p className="section__eyebrow">{t("nav.about")}</p>
          <h2 className="section__title">{t("whyUs.title")}</h2>
        </div>
        <div className="container grid grid--3">
          {WHY_US_ITEMS.map((key) => (
            <div className="content-card" key={key}>
              <h3 className="content-card__title">{t(`whyUs.${key}.title`)}</h3>
              <p className="content-card__desc">{t(`whyUs.${key}.desc`)}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="section section--elevated">
        <div className="container section--center">
          <h2 className="section__title">{t("howItWorks.title")}</h2>
        </div>
        <div className="container how-it-works">
          {(["step1", "step2", "step3", "step4"] as const).map((step, index) => (
            <div className="how-it-works__step" key={step}>
              <div className="how-it-works__number">0{index + 1}</div>
              <p className="how-it-works__title">{t(`howItWorks.${step}.title`)}</p>
              <p className="how-it-works__desc">{t(`howItWorks.${step}.desc`)}</p>
            </div>
          ))}
        </div>
      </section>

      {company && company.operatingCountryCodes.length > 0 && (
        <section className="section section--elevated">
          <div className="container section--center">
            <p className="section__eyebrow">{t("operatingArea.title")}</p>
            <h2 className="section__title">{t("operatingArea.subtitle")}</h2>
          </div>
          <div className="container">
            <ul style={{ listStyle: "none", padding: 0, display: "flex", gap: "var(--space-6)", justifyContent: "center", flexWrap: "wrap" }}>
              {company.operatingCountryCodes.map((code) => (
                <li className="text-secondary" key={code}>{t(`operatingArea.countries.${code}`, { defaultValue: code })}</li>
              ))}
            </ul>
          </div>
        </section>
      )}

      <section className="final-cta">
        <div className="container container--medium">
          <h2 className="section__title">{t("finalCta.title")}</h2>
          <p className="section__subtitle" style={{ marginLeft: "auto", marginRight: "auto" }}>{t("finalCta.subtitle")}</p>
          <Link to="/booking"><button type="button" style={{ fontSize: "0.9rem", padding: "0.9em 2.2em" }}>{t("finalCta.cta")}</button></Link>
        </div>
      </section>

      <Footer />
      <MobileBookingCta />
    </div>
  );
}

export default HomePage;
