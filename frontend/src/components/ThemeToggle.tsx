import { useTranslation } from "react-i18next";
import { useTheme } from "../context/ThemeContext";

/**
 * Public-site-only dark/light switch — a single icon button (a sun/moon
 * glyph swap) rather than a two-option group, since a single toggle is the
 * more conventional pattern for a binary light/dark switch. Deliberately
 * NOT shared with `AdminNav`/`DriverNav` the way `LanguageSelector` is —
 * light mode is specifically a public-site alternative, not something
 * Admin/Driver were asked to have.
 */
function ThemeToggle() {
  const { t } = useTranslation("common");
  const { theme, toggleTheme } = useTheme();
  const isDark = theme === "dark";

  return (
    <button
      type="button"
      className="theme-toggle"
      aria-label={isDark ? t("themeToggle.switchToLight") : t("themeToggle.switchToDark")}
      onClick={toggleTheme}
    >
      <span aria-hidden="true">{isDark ? "☀" : "☾"}</span>
    </button>
  );
}

export default ThemeToggle;
