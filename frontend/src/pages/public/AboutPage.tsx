import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import MobileBookingCta from "../../components/MobileBookingCta";
import { usePageMeta } from "../../hooks/usePageMeta";

const WHY_US_ITEMS = ["professional", "premium", "reliable", "comfortable", "personalized", "simple"] as const;

function AboutPage() {
  const { t } = useTranslation("site");
  usePageMeta(`${t("about.title")} | ${t("footer.description")}`, t("about.subtitle"));

  return (
    <div>
      <Header />

      <section className="section section--center">
        <div className="container container--medium">
          <p className="section__eyebrow">{t("nav.about")}</p>
          <h1 className="section__title">{t("about.title")}</h1>
          <p className="section__subtitle" style={{ marginLeft: "auto", marginRight: "auto" }}>{t("about.subtitle")}</p>
          <p className="text-secondary">{t("about.body1")}</p>
          <p className="text-secondary">{t("about.body2")}</p>
        </div>
      </section>

      <section className="section section--elevated">
        <div className="container section--center">
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

      <section className="final-cta">
        <div className="container container--medium">
          <h2 className="section__title">{t("finalCta.title")}</h2>
          <Link to="/booking"><button type="button">{t("finalCta.cta")}</button></Link>
        </div>
      </section>

      <Footer />
      <MobileBookingCta />
    </div>
  );
}

export default AboutPage;
