import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it } from "vitest";
import LanguageSelector from "./LanguageSelector";
import { AuthProvider } from "../context/AuthContext";
import i18n, { DEFAULT_LANGUAGE, LANGUAGE_STORAGE_KEY } from "../i18n/i18n";

function renderSelector() {
  return render(
    <AuthProvider>
      <LanguageSelector />
    </AuthProvider>
  );
}

describe("LanguageSelector", () => {
  afterEach(async () => {
    await i18n.changeLanguage(DEFAULT_LANGUAGE);
    localStorage.removeItem(LANGUAGE_STORAGE_KEY);
  });

  it("renders one option per supported language", () => {
    renderSelector();

    expect(screen.getByRole("button", { name: "EN" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "DE" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "FR" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "IT" })).not.toBeInTheDocument();
  });

  it("marks the currently active language", () => {
    renderSelector();

    expect(screen.getByRole("button", { name: "EN" })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: "DE" })).toHaveAttribute("aria-pressed", "false");
  });

  it("switches the active language when a different option is clicked", async () => {
    const user = userEvent.setup();
    renderSelector();

    await user.click(screen.getByRole("button", { name: "DE" }));

    expect(i18n.resolvedLanguage).toBe("de");
    expect(screen.getByRole("button", { name: "DE" })).toHaveAttribute("aria-pressed", "true");
  });

  it("persists the choice to localStorage for anonymous visitors", async () => {
    const user = userEvent.setup();
    renderSelector();

    await user.click(screen.getByRole("button", { name: "FR" }));

    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe("fr");
  });

  it("has an accessible group label", () => {
    renderSelector();

    expect(screen.getByRole("group", { name: "Select language" })).toBeInTheDocument();
  });
});
