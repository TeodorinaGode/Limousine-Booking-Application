import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languagedetector";

import enCommon from "../locales/en/common.json";
import enBooking from "../locales/en/booking.json";
import enValidation from "../locales/en/validation.json";
import enPayment from "../locales/en/payment.json";
import enAdmin from "../locales/en/admin.json";
import enDriver from "../locales/en/driver.json";
import enReports from "../locales/en/reports.json";
import enSite from "../locales/en/site.json";

import deCommon from "../locales/de/common.json";
import deBooking from "../locales/de/booking.json";
import deValidation from "../locales/de/validation.json";
import dePayment from "../locales/de/payment.json";
import deAdmin from "../locales/de/admin.json";
import deDriver from "../locales/de/driver.json";
import deReports from "../locales/de/reports.json";
import deSite from "../locales/de/site.json";

import frCommon from "../locales/fr/common.json";
import frBooking from "../locales/fr/booking.json";
import frValidation from "../locales/fr/validation.json";
import frPayment from "../locales/fr/payment.json";
import frAdmin from "../locales/fr/admin.json";
import frDriver from "../locales/fr/driver.json";
import frReports from "../locales/fr/reports.json";
import frSite from "../locales/fr/site.json";

/** The three languages this application supports (Prompt 16) — keep in sync with the backend's SupportedLanguages.Codes. */
export const SUPPORTED_LANGUAGES = ["en", "de", "fr"] as const;
export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number];

export const DEFAULT_LANGUAGE: SupportedLanguage = "en";

export const LANGUAGE_STORAGE_KEY = "limousine-booking.language";

/**
 * All three languages are bundled at build time (not lazy-loaded per language) —
 * the combined JSON payload here is a few KB, far below the threshold where
 * splitting would meaningfully help load time, and bundling avoids an extra
 * network round trip (and a loading flash) every time a customer switches
 * language mid-session. See README's Prompt 16 notes for the trade-off.
 */
i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources: {
      en: { common: enCommon, booking: enBooking, validation: enValidation, payment: enPayment, admin: enAdmin, driver: enDriver, reports: enReports, site: enSite },
      de: { common: deCommon, booking: deBooking, validation: deValidation, payment: dePayment, admin: deAdmin, driver: deDriver, reports: deReports, site: deSite },
      fr: { common: frCommon, booking: frBooking, validation: frValidation, payment: frPayment, admin: frAdmin, driver: frDriver, reports: frReports, site: frSite },
    },
    ns: ["common", "booking", "validation", "payment", "admin", "driver", "reports", "site"],
    defaultNS: "common",
    supportedLngs: [...SUPPORTED_LANGUAGES],
    fallbackLng: DEFAULT_LANGUAGE,
    nonExplicitSupportedLngs: true,
    detection: {
      // localStorage/cookie first (an explicit past choice always wins), then the
      // browser's own language list — never IP-based geolocation (section 2).
      order: ["localStorage", "navigator"],
      lookupLocalStorage: LANGUAGE_STORAGE_KEY,
      caches: ["localStorage"],
    },
    interpolation: { escapeValue: false },
    returnEmptyString: false,
    saveMissing: import.meta.env.DEV,
    missingKeyHandler: import.meta.env.DEV
      ? (_langs, ns, key) => {
          console.warn(`Missing translation: ${ns}.${key}`);
        }
      : undefined,
  });

export default i18n;
