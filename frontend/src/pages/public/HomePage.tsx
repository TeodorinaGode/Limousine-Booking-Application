import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { APP_BRAND_NAME } from "../../config/brand";
import LanguageSelector from "../../components/LanguageSelector";

function HomePage() {
  const { t } = useTranslation(["common", "booking"]);

  return (
    <div>
      <div className="container">
        <nav className="site-nav">
          <span className="site-nav__brand">{APP_BRAND_NAME}</span>
          <div className="row" style={{ alignItems: "center" }}>
            <LanguageSelector />
            <Link to="/booking">
              <span className="btn-secondary" style={{ display: "inline-block", padding: "0.5em 1.2em", borderRadius: "var(--radius-sm)", fontSize: "0.8rem", fontWeight: 600 }}>
                {t("nav.bookARide")}
              </span>
            </Link>
          </div>
        </nav>
      </div>

      <section className="hero fade-in">
        <div className="container container--medium">
          <p className="hero__eyebrow">{t("booking:hero.eyebrow")}</p>
          <h1 className="hero__title">
            {t("booking:hero.title1")}
            <br />
            {t("booking:hero.title2")}
          </h1>
          <p className="hero__subtitle" style={{ marginLeft: "auto", marginRight: "auto" }}>
            {t("booking:hero.subtitle")}
          </p>
          <Link to="/booking">
            <button type="button" style={{ fontSize: "0.9rem", padding: "0.9em 2.2em" }}>
              {t("booking:title")}
            </button>
          </Link>
        </div>
      </section>

      <section className="container container--medium" style={{ paddingBottom: "var(--space-16)", textAlign: "center" }}>
        <p className="text-muted" style={{ fontSize: "0.8rem", textTransform: "uppercase", letterSpacing: "0.08em" }}>
          {t("booking:hero.strip")}
        </p>
      </section>
    </div>
  );
}

export default HomePage;
