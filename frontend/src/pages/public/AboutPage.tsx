import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import MobileBookingCta from "../../components/MobileBookingCta";
import { usePageMeta } from "../../hooks/usePageMeta";
import { useScrollReveal } from "../../hooks/useScrollReveal";

const WHY_US_ITEMS = ["reliability", "luxury", "professionalChauffeurs"] as const;

function AboutPage() {
  const { t } = useTranslation("site");
  usePageMeta(`${t("about.title")} | ${t("footer.description")}`, t("about.subtitle"));

  const introReveal = useScrollReveal<HTMLElement>();
  const whyUsReveal = useScrollReveal<HTMLElement>();
  const finalCtaReveal = useScrollReveal<HTMLElement>();

  return (
    <div>
      <Header />

      <section ref={introReveal.ref} className={`noir-section noir-section--black section--center ${introReveal.className}`}>
        <div className="container container--medium reveal-lines">
          <p className="noir-label">{t("noir.about.label")}</p>
          <h1 className="noir-heading" style={{ marginTop: "var(--space-4)" }}>
            <span className="strong">{t("about.title")}</span>
          </h1>
          <p className="section__subtitle" style={{ marginLeft: "auto", marginRight: "auto", marginTop: "var(--space-8)" }}>{t("about.subtitle")}</p>
          <p className="text-secondary">{t("about.body1")}</p>
          <p className="text-secondary">{t("about.body2")}</p>
        </div>
      </section>

      <section ref={whyUsReveal.ref} className={`section section--elevated ${whyUsReveal.className}`}>
        <div className="container section--center">
          <h2 className="section__title">{t("whyUs.title")}</h2>
        </div>
        <div className="container grid grid--bento">
          {WHY_US_ITEMS.map((key) => (
            <div className="content-card" key={key}>
              <h3 className="content-card__title">{t(`whyUs.${key}.title`)}</h3>
              <p className="content-card__desc">{t(`whyUs.${key}.desc`)}</p>
            </div>
          ))}
        </div>
      </section>

      <section ref={finalCtaReveal.ref} className={`final-cta ${finalCtaReveal.className}`}>
        <div className="container container--medium">
          <h2 className="section__title">{t("finalCta.title")}</h2>
          <Link to="/booking" className="cta-noir cta-noir--solid">{t("finalCta.cta")}<span aria-hidden="true"> &rarr;</span></Link>
        </div>
      </section>

      <Footer />
      <MobileBookingCta />
    </div>
  );
}

export default AboutPage;
