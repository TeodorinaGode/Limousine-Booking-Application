import { useTranslation } from "react-i18next";
import Header from "../../components/Header";
import Footer from "../../components/Footer";
import { usePageMeta } from "../../hooks/usePageMeta";

interface LegalPageProps {
  titleKey: "privacyTitle" | "termsTitle" | "cookieTitle";
}

/** Shared layout for the three legal placeholder pages (Prompt 17, section 24) — no invented legal text (section 24/48); the actual copy is added here once the company provides it. */
function LegalPage({ titleKey }: LegalPageProps) {
  const { t } = useTranslation("site");
  usePageMeta(`${t(`legal.${titleKey}`)} | ${t("footer.description")}`);

  return (
    <div>
      <Header />
      <section className="section">
        <div className="container container--medium">
          <h1 className="section__title">{t(`legal.${titleKey}`)}</h1>
          <p className="text-secondary">{t("legal.placeholder")}</p>
        </div>
      </section>
      <Footer />
    </div>
  );
}

export default LegalPage;
