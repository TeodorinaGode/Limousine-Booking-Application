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
import { useScrollReveal } from "../../hooks/useScrollReveal";
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

/**
 * Public homepage, redesigned to a dark cinematic identity (Prompt 21, later
 * extended with a real hero photograph, About/FAQ/Contact teasers, and a
 * card-free "Why ROI" treatment). The hero uses a real, freely-licensed
 * (Unsplash License) black Mercedes-Benz S-Class night photo — see
 * public/images/hero-black-mercedes-night(.jpg|-mobile.jpg) and the credit
 * in README. Fleet/chauffeur sections still render an atmospheric gradient
 * panel in place of a photo (no licensed asset exists for those yet — see
 * README known gaps). All routes/vehicles/locations/company data below still
 * comes from the same live backend calls as before (section 41) — only the
 * visual presentation changed.
 */
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

  const introReveal = useScrollReveal<HTMLElement>();
  const trustReveal = useScrollReveal<HTMLElement>();
  const routesReveal = useScrollReveal<HTMLElement>();
  const mapReveal = useScrollReveal<HTMLElement>();
  const fleetReveal = useScrollReveal<HTMLElement>();
  const experienceReveal = useScrollReveal<HTMLElement>();
  const chauffeurReveal = useScrollReveal<HTMLElement>();
  const whyUsReveal = useScrollReveal<HTMLElement>();
  const howItWorksReveal = useScrollReveal<HTMLElement>();
  const operatingAreaReveal = useScrollReveal<HTMLElement>();
  const aboutTeaserReveal = useScrollReveal<HTMLElement>();
  const faqTeaserReveal = useScrollReveal<HTMLElement>();
  const contactTeaserReveal = useScrollReveal<HTMLElement>();
  const finalHeroReveal = useScrollReveal<HTMLElement>();

  const featuredVehicle = vehicles[0];

  return (
    <div>
      <Header />

      <section className="cinematic-hero">
        <div className="cinematic-hero__scrim" aria-hidden="true" />
        <div className="container cinematic-hero__content">
          <p className="cinematic-hero__mark">{t("hero.eyebrow")}</p>
          <h1 className="cinematic-hero__title">
            <span className="cinematic-hero__title-light">{t("noir.hero.title1")}</span>
            <span className="cinematic-hero__title-strong">{t("noir.hero.title2")}</span>
          </h1>
          <div className="cinematic-hero__actions">
            <Link to="/booking" className="cta-noir cta-noir--solid">
              {t("noir.ctaBeginJourney")}<span aria-hidden="true"> &rarr;</span>
            </Link>
            <Link to="/services" className="cta-noir cta-noir--outline">
              {t("hero.ctaSecondary")}
            </Link>
          </div>
        </div>
      </section>

      <section ref={introReveal.ref} className={`noir-section noir-section--black ${introReveal.className}`}>
        <div className={`container container--medium reveal-lines ${introReveal.className}`}>
          <p className="noir-label">{t("noir.intro.label")}</p>
          <h2 className="noir-heading">
            <span className="light">{t("noir.intro.title1")}</span>
            <span className="strong">{t("noir.intro.title2")}</span>
          </h2>
          <p className="section__subtitle" style={{ fontSize: "1.2rem", color: "var(--color-text-primary)", marginTop: "var(--space-8)" }}>
            &ldquo;{t("noir.intro.quote")}&rdquo;
          </p>
          <p className="text-secondary">{t("noir.intro.body")}</p>
        </div>
      </section>

      <section ref={trustReveal.ref} className={`section section--tight ${trustReveal.className}`}>
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

      <section ref={routesReveal.ref} className={`section ${routesReveal.className}`}>
        <div className="container">
          <HeroBookingWidget routes={routes} />
        </div>
      </section>

      {locationsData && locationsData.enabled && locationsData.locations.length > 0 && (
        <section ref={mapReveal.ref} className={`section ${mapReveal.className}`}>
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

      {featuredVehicle && (
        <section ref={fleetReveal.ref} className={`noir-section noir-section--deep ${fleetReveal.className}`}>
          <div className="container">
            <div className={`fleet-cinematic ${fleetReveal.className}`}>
              <div>
                <p className="fleet-cinematic__label">{t("nav.fleet")}</p>
                <h2 className="fleet-cinematic__name">
                  <span style={{ display: "block" }}>{featuredVehicle.make}</span>
                  <span style={{ display: "block" }}>{featuredVehicle.model}</span>
                </h2>
                <div className="row" style={{ marginTop: "var(--space-6)" }}>
                  <Link to="/fleet" className="cta-noir cta-noir--outline">{t("routesTeaser.viewAll")}</Link>
                  <Link to="/booking" className="cta-noir cta-noir--solid">
                    {t("fleetTeaser.bookYourJourney")}<span aria-hidden="true"> &rarr;</span>
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </section>
      )}

      <section ref={experienceReveal.ref} className={`noir-section noir-section--black ${experienceReveal.className}`}>
        <div className="container container--medium section--center">
          <p className="section__eyebrow">{t("noir.experience.eyebrow")}</p>
          <div className={`reveal-lines noir-heading ${experienceReveal.className}`} style={{ marginTop: "var(--space-8)" }}>
            {(t("noir.experience.words", { returnObjects: true }) as string[]).map((word) => (
              <span className="strong" key={word} style={{ display: "block" }}>{word}</span>
            ))}
          </div>
        </div>
      </section>

      <section ref={chauffeurReveal.ref} className={`section section--elevated ${chauffeurReveal.className}`}>
        <div className="container row" style={{ alignItems: "center", gap: "var(--space-8)", flexWrap: "wrap" }}>
          <div className="noir-portrait" style={{ flex: "1 1 320px" }} role="img" aria-label={t("noir.chauffeur.title1")} />
          <div style={{ flex: "1 1 320px" }}>
            <h2 className="noir-heading">
              <span className="light">{t("noir.chauffeur.title1")}</span>
              <span className="strong">{t("noir.chauffeur.title2")}</span>
              <span className="strong">{t("noir.chauffeur.title3")}</span>
            </h2>
            <p className="text-secondary" style={{ marginTop: "var(--space-4)" }}>{t("noir.chauffeur.body")}</p>
          </div>
        </div>
      </section>

      <section ref={whyUsReveal.ref} className={`section ${whyUsReveal.className}`}>
        <div className="container section--center">
          <p className="section__eyebrow">{t("nav.about")}</p>
          <h2 className="section__title">{t("whyUs.title")}</h2>
        </div>
        <div className="container pillars">
          {WHY_US_ITEMS.map((key, index) => (
            <div className="pillars__item" key={key}>
              <span className="pillars__index">0{index + 1}</span>
              <h3 className="pillars__title">{t(`whyUs.${key}.title`)}</h3>
              <p className="pillars__desc">{t(`whyUs.${key}.desc`)}</p>
            </div>
          ))}
        </div>
      </section>

      <section ref={howItWorksReveal.ref} className={`section section--elevated ${howItWorksReveal.className}`}>
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
        <section ref={operatingAreaReveal.ref} className={`noir-section noir-section--black ${operatingAreaReveal.className}`}>
          <div className="container section--center">
            <h2 className="noir-heading">
              <span className="strong">{t("noir.operatingArea.title1")}</span>
              <span className="light">{t("noir.operatingArea.title2")}</span>
            </h2>
          </div>
          <div className="container" style={{ marginTop: "var(--space-8)" }}>
            <ul className={`operating-list reveal-lines ${operatingAreaReveal.className}`}>
              {company.operatingCountryCodes.map((code) => (
                <li key={code}>{t(`operatingArea.countries.${code}`, { defaultValue: code })}</li>
              ))}
            </ul>
          </div>
        </section>
      )}

      <section ref={aboutTeaserReveal.ref} className={`section ${aboutTeaserReveal.className}`}>
        <div className="container row" style={{ alignItems: "center", gap: "var(--space-8)", flexWrap: "wrap" }}>
          <div style={{ flex: "1 1 320px" }}>
            <p className="section__eyebrow">{t("homeTeasers.about.label")}</p>
            <h2 className="section__title">{t("about.title")}</h2>
            <p className="text-secondary">{t("about.subtitle")}</p>
          </div>
          <div style={{ flex: "0 0 auto" }}>
            <Link to="/about" className="cta-noir cta-noir--outline">
              {t("homeTeasers.about.cta")}<span aria-hidden="true"> &rarr;</span>
            </Link>
          </div>
        </div>
      </section>

      <section ref={faqTeaserReveal.ref} className={`section section--elevated ${faqTeaserReveal.className}`}>
        <div className="container section--center">
          <p className="section__eyebrow">{t("homeTeasers.faq.label")}</p>
          <h2 className="section__title">{t("faq.title")}</h2>
        </div>
        <div className="container container--medium">
          {(["q1", "q2", "q3"] as const).map((key) => (
            <details className="faq-item" key={key}>
              <summary className="faq-item__question">{t(`faq.${key}.question`)}</summary>
              <p className="faq-item__answer">{t(`faq.${key}.answer`)}</p>
            </details>
          ))}
          <div className="section--center" style={{ marginTop: "var(--space-8)" }}>
            <Link to="/faq" className="cta-noir cta-noir--outline">
              {t("homeTeasers.faq.cta")}<span aria-hidden="true"> &rarr;</span>
            </Link>
          </div>
        </div>
      </section>

      {company && (
        <section ref={contactTeaserReveal.ref} className={`noir-section noir-section--deep section--center ${contactTeaserReveal.className}`}>
          <div className="container container--medium">
            <p className="noir-label">{t("homeTeasers.contact.label")}</p>
            <h2 className="noir-heading" style={{ marginTop: "var(--space-4)" }}>
              <span className="light">{t("noir.contact.title2")}</span>
              <span className="strong">{t("noir.contact.title3")}</span>
            </h2>
            <div className="row" style={{ justifyContent: "center", marginTop: "var(--space-8)" }}>
              <a href={`tel:${company.phone}`} className="btn-secondary" style={{ display: "inline-block", padding: "0.6em 1.2em", borderRadius: "var(--radius-sm)" }}>
                {t("contact.callUs")}
              </a>
              <Link to="/contact" className="cta-noir cta-noir--solid">
                {t("homeTeasers.contact.cta")}<span aria-hidden="true"> &rarr;</span>
              </Link>
            </div>
          </div>
        </section>
      )}

      <section ref={finalHeroReveal.ref} className={`noir-section noir-section--black section--center ${finalHeroReveal.className}`} style={{ paddingTop: "var(--space-16)", paddingBottom: "var(--space-16)" }}>
        <div className="container container--medium">
          <h2 className="noir-heading">
            <span className="light">{t("noir.finalHero.title1")}</span>
            <span className="strong">{t("noir.finalHero.title2")}</span>
          </h2>
          <div style={{ marginTop: "var(--space-8)" }}>
            <Link to="/booking" className="cta-noir cta-noir--solid">
              {t("nav.bookARide")}<span aria-hidden="true"> &rarr;</span>
            </Link>
          </div>
        </div>
      </section>

      <Footer />
      <MobileBookingCta />
    </div>
  );
}

export default HomePage;
