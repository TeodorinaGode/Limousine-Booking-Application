import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ThemeProvider, useTheme } from "./ThemeContext";

const STORAGE_KEY = "limousine-booking.theme";

function Probe() {
  const { theme, toggleTheme } = useTheme();
  return (
    <button type="button" onClick={toggleTheme}>
      {theme}
    </button>
  );
}

afterEach(() => {
  localStorage.removeItem(STORAGE_KEY);
});

describe("ThemeContext", () => {
  it("defaults to dark when nothing is stored", () => {
    render(
      <ThemeProvider>
        <Probe />
      </ThemeProvider>
    );

    expect(screen.getByRole("button")).toHaveTextContent("dark");
  });

  it("toggling switches the theme and persists it to localStorage", async () => {
    const user = userEvent.setup();
    render(
      <ThemeProvider>
        <Probe />
      </ThemeProvider>
    );

    await user.click(screen.getByRole("button"));

    expect(screen.getByRole("button")).toHaveTextContent("light");
    expect(localStorage.getItem(STORAGE_KEY)).toBe("light");
  });

  it("reads a previously persisted theme on mount", () => {
    localStorage.setItem(STORAGE_KEY, "light");

    render(
      <ThemeProvider>
        <Probe />
      </ThemeProvider>
    );

    expect(screen.getByRole("button")).toHaveTextContent("light");
  });

  it("throws when useTheme is called outside a ThemeProvider", () => {
    // Suppress the expected React error-boundary console noise for this one assertion.
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => undefined);

    expect(() => render(<Probe />)).toThrow("useTheme must be used within a ThemeProvider");

    consoleError.mockRestore();
  });
});
