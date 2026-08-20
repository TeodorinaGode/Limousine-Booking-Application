import { useTranslation } from "react-i18next";
import { useAuth } from "../context/AuthContext";
import { updatePreferences } from "../services/accountService";
import { SUPPORTED_LANGUAGES, type SupportedLanguage } from "../i18n/i18n";

/**
 * Shared EN | DE | FR | IT selector used in the public nav, admin header, and
 * driver header (sections 46-48). Switching language only ever calls
 * i18next's changeLanguage — it never touches booking/form state, so nothing
 * the customer has already entered is lost (section 40). For an authenticated
 * user, the choice is also persisted server-side (section 21) so it's restored
 * on their next login from any device — anonymous visitors only get the
 * localStorage persistence i18next's own detector already provides.
 */
function LanguageSelector() {
  const { t, i18n } = useTranslation("common");
  const { isAuthenticated, accessToken } = useAuth();

  const currentLanguage = (i18n.resolvedLanguage ?? "en") as SupportedLanguage;

  const handleSelect = (language: SupportedLanguage) => {
    if (language === currentLanguage) return;
    i18n.changeLanguage(language);

    if (isAuthenticated && accessToken) {
      updatePreferences({ languageCode: language }, accessToken).catch(() => {
        // Best-effort — the UI has already switched language locally regardless.
      });
    }
  };

  return (
    <div className="language-selector" role="group" aria-label={t("languageSelector.label")}>
      {SUPPORTED_LANGUAGES.map((language) => (
        <button
          key={language}
          type="button"
          className={`language-selector__option${language === currentLanguage ? " language-selector__option--active" : ""}`}
          aria-pressed={language === currentLanguage}
          onClick={() => handleSelect(language)}
        >
          {t(`languageSelector.${language}`)}
        </button>
      ))}
    </div>
  );
}

export default LanguageSelector;
