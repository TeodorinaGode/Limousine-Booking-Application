import { useEffect, useState } from "react";

const STORAGE_KEY = "limousine-booking.sidebar-collapsed";

// Matches the ≤900px breakpoint where AdminNav/DriverNav switch to a
// horizontal mobile bar (index.css) — the manual collapse toggle is hidden
// there, so a collapsed state saved from a desktop session must never carry
// over and leave someone on a narrow viewport stuck with an icon-only bar
// and no way to toggle it back.
const MOBILE_QUERY = "(max-width: 900px)";

function readStored(): boolean {
  return localStorage.getItem(STORAGE_KEY) === "true";
}

/** Guards environments without matchMedia (older browsers, the jsdom test environment) — treat as "not mobile" rather than throwing. */
function matchesMobile(): boolean {
  return typeof window.matchMedia === "function" && window.matchMedia(MOBILE_QUERY).matches;
}

/** Shared collapse state for AdminNav/DriverNav — persisted so a collapsed sidebar stays collapsed across page loads, same pattern as the old theme preference. */
function useSidebarCollapse(): [boolean, () => void] {
  const [isCollapsedPreference, setIsCollapsedPreference] = useState<boolean>(readStored);
  const [isMobile, setIsMobile] = useState<boolean>(matchesMobile);

  useEffect(() => {
    if (typeof window.matchMedia !== "function") return;

    const mediaQuery = window.matchMedia(MOBILE_QUERY);
    const handleChange = (event: MediaQueryListEvent) => setIsMobile(event.matches);
    mediaQuery.addEventListener("change", handleChange);
    return () => mediaQuery.removeEventListener("change", handleChange);
  }, []);

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, String(isCollapsedPreference));
  }, [isCollapsedPreference]);

  const toggle = () => setIsCollapsedPreference((current) => !current);

  return [isMobile ? false : isCollapsedPreference, toggle];
}

export default useSidebarCollapse;
