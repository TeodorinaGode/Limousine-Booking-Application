import { useTranslation } from "react-i18next";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import MobileBookingCta from "../../components/MobileBookingCta";
import { usePageMeta } from "../../hooks/usePageMeta";

const FAQ_KEYS = ["q1", "q2", "q3", "q4", "q5", "q6", "q7", "q8"] as const;

/** Public FAQ page (Prompt 17, section 20) — uses native <details>/<summary> for the accordion, which is keyboard/screen-reader accessible by default without any extra ARIA wiring (section 35). Content comes from site.json, translated in all three languages — never invented policies (section 20/48). */
function FAQPage() {
  const { t } = useTranslation("site");
  usePageMeta(`${t("faq.title")} | ${t("footer.description")}`);

  return (
    <div>
      <Header />

      <section className="section section--center">
        <div className="container container--medium">
          <p className="section__eyebrow">{t("nav.faq")}</p>
          <h1 className="section__title">{t("faq.title")}</h1>
        </div>
      </section>

      <section className="section" style={{ paddingTop: 0 }}>
        <div className="container container--medium">
          {FAQ_KEYS.map((key) => (
            <details className="faq-item" key={key}>
              <summary className="faq-item__question">{t(`faq.${key}.question`)}</summary>
              <p className="faq-item__answer">{t(`faq.${key}.answer`)}</p>
            </details>
          ))}
        </div>
      </section>

      <Footer />
      <MobileBookingCta />
    </div>
  );
}

export default FAQPage;
