import { createContext, useContext, useEffect, useState, type ReactNode } from "react";

export type Theme = "dark" | "light";

interface ThemeContextValue {
  theme: Theme;
  toggleTheme: () => void;
}

const STORAGE_KEY = "limousine-booking.theme";
const DEFAULT_THEME: Theme = "dark";

function readStoredTheme(): Theme {
  const raw = localStorage.getItem(STORAGE_KEY);
  return raw === "light" || raw === "dark" ? raw : DEFAULT_THEME;
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

/**
 * Dark stays the default (the cinematic identity from Prompt 21 is the
 * brand's first impression) — light is an opt-in alternative a visitor can
 * toggle and have remembered via localStorage, same persistence pattern as
 * `AuthContext`'s saved session. The provider only ever holds state; it does
 * NOT touch `document.documentElement` itself — see `Header`'s mount effect,
 * which is the one place `data-theme` actually gets applied, and only for as
 * long as a public page (which always renders `Header`) is on screen. Admin
 * and Driver pages never render `Header`, so a visitor's saved light-theme
 * preference can never bleed into those portals, even though this context
 * itself is provided app-wide for simplicity.
 */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<Theme>(readStoredTheme);

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, theme);
  }, [theme]);

  const toggleTheme = () => setTheme((current) => (current === "dark" ? "light" : "dark"));

  return <ThemeContext.Provider value={{ theme, toggleTheme }}>{children}</ThemeContext.Provider>;
}

export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext);
  if (!context) {
    throw new Error("useTheme must be used within a ThemeProvider");
  }
  return context;
}
