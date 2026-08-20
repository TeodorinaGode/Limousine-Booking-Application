import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import MobileBookingCta from "../../components/MobileBookingCta";
import { usePageMeta } from "../../hooks/usePageMeta";
import { SERVICES } from "../../config/services";

function ServicesPage() {
  const { t } = useTranslation("site");
  usePageMeta(`${t("services.title")} | ${t("footer.description")}`, t("services.subtitle"));

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

      <section className="section" style={{ paddingTop: 0 }}>
        <div className="container grid grid--3">
          {SERVICES.map((service) => (
            <article className="content-card" key={service.key}>
              <div className="content-card__media" role="img" aria-label={t(`services.${service.key}.title`)}>{service.icon}</div>
              <h2 className="content-card__title" style={{ textTransform: "none" }}>{t(`services.${service.key}.title`)}</h2>
              <p className="content-card__desc">{t(`services.${service.key}.desc`)}</p>
              <Link to="/booking" className="btn-secondary" style={{ textAlign: "center", padding: "0.6em", borderRadius: "var(--radius-sm)" }}>
                {t("services.bookTransfer")}
              </Link>
            </article>
          ))}
        </div>
      </section>

      <section className="section section--elevated">
        <div className="container container--medium section--center">
          <p className="section__eyebrow">{t("nav.services")}</p>
          <h2 className="section__title">{t("airportHighlight.title")}</h2>
          <p className="section__subtitle" style={{ marginLeft: "auto", marginRight: "auto" }}>{t("airportHighlight.subtitle")}</p>
          <ul style={{ listStyle: "none", padding: 0, display: "flex", gap: "var(--space-6)", justifyContent: "center", flexWrap: "wrap", marginBottom: "var(--space-8)" }}>
            <li className="text-secondary">{t("airportHighlight.pickup")}</li>
            <li className="text-secondary">{t("airportHighlight.chauffeur")}</li>
            <li className="text-secondary">{t("airportHighlight.vehicles")}</li>
          </ul>
          <Link to="/booking"><button type="button">{t("airportHighlight.cta")}</button></Link>
        </div>
      </section>

      <Footer />
      <MobileBookingCta />
    </div>
  );
}

export default ServicesPage;
