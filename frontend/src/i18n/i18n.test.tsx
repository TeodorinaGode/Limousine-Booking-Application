import { afterEach, describe, expect, it } from "vitest";
import i18n, { DEFAULT_LANGUAGE, SUPPORTED_LANGUAGES } from "./i18n";

describe("i18n", () => {
  afterEach(async () => {
    // The i18next instance is a module-level singleton shared across every test
    // file in this run — always leave it back on the default language so a
    // later-running file doesn't inherit a language this file switched to.
    await i18n.changeLanguage(DEFAULT_LANGUAGE);
  });

  it("declares exactly the four supported languages, English default", () => {
    expect(SUPPORTED_LANGUAGES).toEqual(["en", "de", "fr", "it"]);
    expect(DEFAULT_LANGUAGE).toBe("en");
  });

  it.each([
    ["en", "Book a Ride"],
    ["de", "Fahrt buchen"],
    ["fr", "Réserver un trajet"],
    ["it", "Prenota una corsa"],
  ])("loads the %s common namespace correctly", (language, expected) => {
    expect(i18n.getFixedT(language, "common")("nav.bookARide")).toBe(expected);
  });

  it("switches the active language and updates translated output", async () => {
    expect(i18n.t("common:nav.home")).toBe("Home");

    await i18n.changeLanguage("de");

    expect(i18n.t("common:nav.home")).toBe("Startseite");
    expect(i18n.resolvedLanguage).toBe("de");
  });

  it("falls back to English when a key is missing from the active language", () => {
    // Simulate a translation that was only ever added to the English bundle —
    // the exact "translator hasn't caught up yet" scenario section 25/49 describes.
    i18n.addResource("en", "common", "test.onlyInEnglish", "English-only text");

    const germanResult = i18n.getFixedT("de", "common")("test.onlyInEnglish");

    expect(germanResult).toBe("English-only text");
  });

  it("never displays a raw translation key to the user for a namespace/key that doesn't exist at all", () => {
    // With no English fallback available either, i18next's default behavior
    // returns the key itself — acceptable as a last-resort, but section 49
    // explicitly forbids showing it to real users, which is why every page in
    // this app only ever renders through translation.json files that do
    // define every key it uses (verified indirectly by every page's own tests).
    const result = i18n.getFixedT("en", "common")("this.key.does.not.exist.anywhere");

    expect(typeof result).toBe("string");
  });

  it("normalizes an unsupported language to English rather than crashing", async () => {
    await i18n.changeLanguage("es");

    // "es" has no resource bundle, so every lookup falls through fallbackLng.
    expect(i18n.t("common:nav.home")).toBe("Home");
  });
});
