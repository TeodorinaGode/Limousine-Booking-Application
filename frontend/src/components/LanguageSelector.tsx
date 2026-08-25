import { useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { useAuth } from "../context/AuthContext";
import { updatePreferences } from "../services/accountService";
import { SUPPORTED_LANGUAGES, type SupportedLanguage } from "../i18n/i18n";

/**
 * Shared language picker used in the public nav, admin header, and driver
 * header (sections 46-48) — a compact dropdown (current language + caret,
 * opening a short menu) rather than three always-visible buttons, matching
 * the theme toggle's single-control footprint and scaling better if more
 * languages get added later. Switching language only ever calls i18next's
 * changeLanguage — it never touches booking/form state, so nothing the
 * customer has already entered is lost (section 40). For an authenticated
 * user, the choice is also persisted server-side (section 21) so it's
 * restored on their next login from any device — anonymous visitors only
 * get the localStorage persistence i18next's own detector already provides.
 */
function LanguageSelector() {
  const { t, i18n } = useTranslation("common");
  const { isAuthenticated, accessToken } = useAuth();
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const currentLanguage = (i18n.resolvedLanguage ?? "en") as SupportedLanguage;

  useEffect(() => {
    if (!isOpen) return;

    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setIsOpen(false);
    };

    document.addEventListener("mousedown", handleClickOutside);
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [isOpen]);

  const handleSelect = (language: SupportedLanguage) => {
    setIsOpen(false);
    if (language === currentLanguage) return;
    i18n.changeLanguage(language);

    if (isAuthenticated && accessToken) {
      updatePreferences({ languageCode: language }, accessToken).catch(() => {
        // Best-effort — the UI has already switched language locally regardless.
      });
    }
  };

  return (
    <div className="language-selector" ref={containerRef}>
      <button
        type="button"
        className="language-selector__trigger"
        aria-haspopup="true"
        aria-expanded={isOpen}
        aria-label={t("languageSelector.label")}
        onClick={() => setIsOpen((open) => !open)}
      >
        {t(`languageSelector.${currentLanguage}`)}
        <span className="language-selector__caret" aria-hidden="true">{isOpen ? "▴" : "▾"}</span>
      </button>
      {isOpen && (
        <div className="language-selector__menu" role="menu">
          {SUPPORTED_LANGUAGES.map((language) => (
            <button
              key={language}
              type="button"
              role="menuitemradio"
              aria-checked={language === currentLanguage}
              className={`language-selector__option${language === currentLanguage ? " language-selector__option--active" : ""}`}
              onClick={() => handleSelect(language)}
            >
              {t(`languageSelector.${language}`)}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

export default LanguageSelector;
