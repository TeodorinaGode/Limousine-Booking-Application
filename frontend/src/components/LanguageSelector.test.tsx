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

  it("shows a single trigger button labelled with the current language", () => {
    renderSelector();

    expect(screen.getByRole("button", { name: "Select language" })).toHaveTextContent("EN");
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
  });

  it("opens a menu listing every supported language when clicked", async () => {
    const user = userEvent.setup();
    renderSelector();

    await user.click(screen.getByRole("button", { name: "Select language" }));

    const menu = screen.getByRole("menu");
    expect(menu).toBeInTheDocument();
    expect(screen.getByRole("menuitemradio", { name: "EN" })).toHaveAttribute("aria-checked", "true");
    expect(screen.getByRole("menuitemradio", { name: "DE" })).toHaveAttribute("aria-checked", "false");
    expect(screen.getByRole("menuitemradio", { name: "FR" })).toBeInTheDocument();
    expect(screen.queryByRole("menuitemradio", { name: "IT" })).not.toBeInTheDocument();
  });

  it("switches the active language when an option is clicked, and closes the menu", async () => {
    const user = userEvent.setup();
    renderSelector();

    await user.click(screen.getByRole("button", { name: "Select language" }));
    await user.click(screen.getByRole("menuitemradio", { name: "DE" }));

    expect(i18n.resolvedLanguage).toBe("de");
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    expect(screen.getByRole("button")).toHaveTextContent("DE");
  });

  it("persists the choice to localStorage for anonymous visitors", async () => {
    const user = userEvent.setup();
    renderSelector();

    await user.click(screen.getByRole("button", { name: "Select language" }));
    await user.click(screen.getByRole("menuitemradio", { name: "FR" }));

    expect(localStorage.getItem(LANGUAGE_STORAGE_KEY)).toBe("fr");
  });

  it("closes the menu when clicking outside", async () => {
    const user = userEvent.setup();
    render(
      <AuthProvider>
        <div>
          <LanguageSelector />
          <button type="button">Outside</button>
        </div>
      </AuthProvider>
    );

    await user.click(screen.getByRole("button", { name: "Select language" }));
    expect(screen.getByRole("menu")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Outside" }));
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
  });

  it("closes the menu when pressing Escape", async () => {
    const user = userEvent.setup();
    renderSelector();

    await user.click(screen.getByRole("button", { name: "Select language" }));
    expect(screen.getByRole("menu")).toBeInTheDocument();

    await user.keyboard("{Escape}");
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
  });
});
