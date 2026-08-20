import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { APP_BRAND_NAME } from "../config/brand";
import { useCompanyInfo } from "../hooks/useCompanyInfo";

/** The complete public-website footer (Prompt 17, section 23) — company summary, navigation, services, and contact details, plus a legal/bottom bar. Only ever lists services that actually exist as marketing categories (section 6/23), never invented ones. */
function Footer() {
  const { t } = useTranslation("site");
  const company = useCompanyInfo();
  const year = new Date().getFullYear();

  return (
    <footer className="site-footer">
      <div className="container site-footer__grid">
        <div>
          <p className="site-footer__brand">{APP_BRAND_NAME}</p>
          <p className="text-secondary" style={{ maxWidth: 280 }}>{t("footer.description")}</p>
        </div>

        <div>
          <h3 className="site-footer__heading">{t("footer.navigation")}</h3>
          <ul className="site-footer__list">
            <li><Link to="/">{t("nav.home")}</Link></li>
            <li><Link to="/services">{t("nav.services")}</Link></li>
            <li><Link to="/routes">{t("nav.routes")}</Link></li>
            <li><Link to="/fleet">{t("nav.fleet")}</Link></li>
            <li><Link to="/about">{t("nav.about")}</Link></li>
            <li><Link to="/contact">{t("nav.contact")}</Link></li>
          </ul>
        </div>

        <div>
          <h3 className="site-footer__heading">{t("footer.services")}</h3>
          <ul className="site-footer__list">
            <li><Link to="/services">{t("services.airportTransfer.title")}</Link></li>
            <li><Link to="/services">{t("services.corporateEvents.title")}</Link></li>
            <li><Link to="/services">{t("services.cityTours.title")}</Link></li>
            <li><Link to="/services">{t("services.pointToPoint.title")}</Link></li>
            <li><Link to="/services">{t("services.professionalChauffeursService.title")}</Link></li>
          </ul>
        </div>

        <div>
          <h3 className="site-footer__heading">{t("footer.contact")}</h3>
          <ul className="site-footer__list">
            {company && (
              <>
                <li><a href={`tel:${company.phone}`}>{company.phone}</a></li>
                <li><a href={`mailto:${company.email}`}>{company.email}</a></li>
                <li className="text-muted">{company.address}</li>
              </>
            )}
          </ul>
          {company && (company.facebookUrl || company.instagramUrl || company.whatsAppUrl) && (
            <>
              <h3 className="site-footer__heading" style={{ marginTop: "var(--space-4)" }}>{t("footer.followUs")}</h3>
              <div className="site-footer__social">
                {company.facebookUrl && (
                  <a href={company.facebookUrl} target="_blank" rel="noopener noreferrer" aria-label="Facebook">Facebook</a>
                )}
                {company.instagramUrl && (
                  <a href={company.instagramUrl} target="_blank" rel="noopener noreferrer" aria-label="Instagram">Instagram</a>
                )}
                {company.whatsAppUrl && (
                  <a href={company.whatsAppUrl} target="_blank" rel="noopener noreferrer" aria-label="WhatsApp">WhatsApp</a>
                )}
              </div>
            </>
          )}
        </div>
      </div>

      <div className="container site-footer__bottom">
        <p className="text-muted" style={{ margin: 0 }}>
          &copy; {year} {APP_BRAND_NAME}. {t("footer.rights")}
        </p>
        <div className="site-footer__legal">
          <Link to="/privacy-policy">{t("footer.privacyPolicy")}</Link>
          <Link to="/terms-and-conditions">{t("footer.termsConditions")}</Link>
          <Link to="/cookie-policy">{t("footer.cookiePolicy")}</Link>
        </div>
      </div>
    </footer>
  );
}

export default Footer;
