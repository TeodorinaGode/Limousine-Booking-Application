import { useEffect, useState } from "react";
import { Link, NavLink } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { APP_BRAND_NAME } from "../config/brand";
import LanguageSelector from "./LanguageSelector";
import ThemeToggle from "./ThemeToggle";
import { useTheme } from "../context/ThemeContext";

const NAV_LINKS = [
  { to: "/", key: "home" },
  { to: "/services", key: "services" },
  { to: "/routes", key: "routes" },
  { to: "/fleet", key: "fleet" },
  { to: "/about", key: "about" },
  { to: "/faq", key: "faq" },
  { to: "/contact", key: "contact" },
] as const;

/**
 * Sticky premium header shared by every public-website page (Prompt 17,
 * section 2) — becomes visually more compact once the page has scrolled past
 * the hero, per the spec's explicit "should become slightly more compact when
 * scrolling." Desktop shows the full nav inline; below 900px it collapses
 * into a hamburger-triggered panel (section 33/34) that still keeps the
 * primary "Book a Ride" CTA reachable without opening the menu.
 */
function Header() {
  const { t } = useTranslation("site");
  const { theme } = useTheme();
  const [isScrolled, setIsScrolled] = useState(false);
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  useEffect(() => {
    const onScroll = () => setIsScrolled(window.scrollY > 24);
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, []);

  // `data-theme` is applied to <html> only while a page rendering `Header`
  // (i.e. a public page) is mounted, and removed on unmount — Admin/Driver
  // pages never render `Header`, so a visitor's saved light-theme choice can
  // never bleed into those portals, even though ThemeProvider sits above the
  // whole app for simplicity (see ThemeContext.tsx).
  useEffect(() => {
    document.documentElement.setAttribute("data-theme", theme);
    return () => document.documentElement.removeAttribute("data-theme");
  }, [theme]);

  const [brandFirst, ...brandRest] = APP_BRAND_NAME.split(" ");

  return (
    <header className={`site-header${isScrolled ? " site-header--scrolled" : ""}`}>
      <div className="container site-header__bar">
        <Link to="/" className="site-header__brand" onClick={() => setIsMenuOpen(false)}>
          <span>{brandFirst}</span>
          {brandRest.length > 0 && <span>{brandRest.join(" ")}</span>}
        </Link>

        <nav className="site-header__nav" aria-label="Primary">
          {NAV_LINKS.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={link.to === "/"}
              className={({ isActive }) => `site-header__link${isActive ? " site-header__link--active" : ""}`}
            >
              {t(`nav.${link.key}`)}
            </NavLink>
          ))}
        </nav>

        <div className="site-header__actions">
          <div className="site-header__language">
            <ThemeToggle />
            <LanguageSelector />
          </div>
          <Link to="/booking" className="site-header__cta">
            {t("nav.bookARide")}<span aria-hidden="true"> &rarr;</span>
          </Link>
          <button
            type="button"
            className="site-header__menu-toggle"
            aria-expanded={isMenuOpen}
            aria-label={isMenuOpen ? t("mobileMenu.close") : t("mobileMenu.open")}
            onClick={() => setIsMenuOpen((open) => !open)}
          >
            <span aria-hidden="true">{isMenuOpen ? "✕" : "☰"}</span>
          </button>
        </div>
      </div>

      {isMenuOpen && (
        <div className="site-header__mobile-panel" role="dialog" aria-label="Primary">
          <nav className="site-header__mobile-nav">
            {NAV_LINKS.map((link) => (
              <NavLink
                key={link.to}
                to={link.to}
                end={link.to === "/"}
                className={({ isActive }) => `site-header__mobile-link${isActive ? " site-header__mobile-link--active" : ""}`}
                onClick={() => setIsMenuOpen(false)}
              >
                {t(`nav.${link.key}`)}
              </NavLink>
            ))}
          </nav>
          <div className="site-header__mobile-footer">
            <ThemeToggle />
            <LanguageSelector />
            <Link to="/booking" className="site-header__cta site-header__cta--block" onClick={() => setIsMenuOpen(false)}>
              {t("nav.bookARide")}<span aria-hidden="true"> &rarr;</span>
            </Link>
          </div>
        </div>
      )}
    </header>
  );
}

export default Header;
