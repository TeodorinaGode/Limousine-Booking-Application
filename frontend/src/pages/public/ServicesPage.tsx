import { useState } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import MobileBookingCta from "../../components/MobileBookingCta";
import { usePageMeta } from "../../hooks/usePageMeta";
import { useScrollReveal } from "../../hooks/useScrollReveal";
import { SERVICES } from "../../config/services";

function ServicesPage() {
  const { t } = useTranslation("site");
  usePageMeta(`${t("services.title")} | ${t("footer.description")}`, t("services.subtitle"));

  const [activeService, setActiveService] = useState(SERVICES[0].key);
  const listReveal = useScrollReveal<HTMLElement>();
  const highlightReveal = useScrollReveal<HTMLElement>();

  return (
    <div>
      <Header />

      <section className="section section--center">
        <div className="container">
          <p className="section__eyebrow">{t("nav.services")}</p>
          <h1 className="section__title">{t("services.title")}</h1>
          <p className="section__subtitle" style={{ marginLeft: "auto", marginRight: "auto" }}>{t("services.subtitle")}</p>
        </div>
      </section>

      <section ref={listReveal.ref} className={`noir-section noir-section--deep ${listReveal.className}`} style={{ paddingTop: 0 }}>
        <div className="container row" style={{ alignItems: "flex-start", gap: "var(--space-8)" }}>
          <div className="editorial-list" style={{ flex: 1, minWidth: 0 }}>
            {SERVICES.map((service, index) => (
              <Link
                key={service.key}
                to="/booking"
                className={`editorial-list__row${activeService === service.key ? " editorial-list__row--active" : ""}`}
                onMouseEnter={() => setActiveService(service.key)}
                onFocus={() => setActiveService(service.key)}
              >
                <span className="editorial-list__index">0{index + 1}</span>
                <span className="editorial-list__title">{t(`services.${service.key}.title`)}</span>
                <span className="editorial-list__desc">{t(`services.${service.key}.desc`)}</span>
              </Link>
            ))}
          </div>
          <div className="editorial-panel" aria-hidden="true">
            {SERVICES.map((service) => (
              <div key={service.key} className={`editorial-panel__image${activeService === service.key ? " editorial-panel__image--active" : ""}`}>
                {t(`services.${service.key}.title`)}
              </div>
            ))}
          </div>
        </div>
      </section>

      <section ref={highlightReveal.ref} className={`section section--elevated ${highlightReveal.className}`}>
        <div className="container container--medium section--center">
          <p className="section__eyebrow">{t("nav.services")}</p>
          <h2 className="section__title">{t("airportHighlight.title")}</h2>
          <p className="section__subtitle" style={{ marginLeft: "auto", marginRight: "auto" }}>{t("airportHighlight.subtitle")}</p>
          <ul style={{ listStyle: "none", padding: 0, display: "flex", gap: "var(--space-6)", justifyContent: "center", flexWrap: "wrap", marginBottom: "var(--space-8)" }}>
            <li className="text-secondary">{t("airportHighlight.pickup")}</li>
            <li className="text-secondary">{t("airportHighlight.chauffeur")}</li>
            <li className="text-secondary">{t("airportHighlight.vehicles")}</li>
          </ul>
          <Link to="/booking" className="cta-noir cta-noir--solid">{t("airportHighlight.cta")}</Link>
        </div>
      </section>

      <Footer />
      <MobileBookingCta />
    </div>
  );
}

export default ServicesPage;
