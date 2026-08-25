import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { afterEach, describe, expect, it } from "vitest";
import Header from "./Header";
import { AuthProvider } from "../context/AuthContext";
import { ThemeProvider } from "../context/ThemeContext";

afterEach(() => {
  localStorage.removeItem("limousine-booking.theme");
  document.documentElement.removeAttribute("data-theme");
});

function renderHeader() {
  return render(
    <MemoryRouter>
      <ThemeProvider><AuthProvider>
        <Header />
      </AuthProvider></ThemeProvider>
    </MemoryRouter>
  );
}

describe("Header", () => {
  it("renders the primary navigation links", () => {
    renderHeader();

    expect(screen.getByRole("link", { name: "Home" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Services" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Routes" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Fleet" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "About" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "FAQ" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Contact" })).toBeInTheDocument();
  });

  it("shows the primary Book a Ride call to action", () => {
    renderHeader();

    const ctaLinks = screen.getAllByRole("link", { name: "Book a Ride" });
    expect(ctaLinks.length).toBeGreaterThan(0);
    expect(ctaLinks[0]).toHaveAttribute("href", "/booking");
  });

  it("opens and closes the mobile menu", async () => {
    const user = userEvent.setup();
    renderHeader();

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Open menu" }));
    expect(screen.getByRole("dialog")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Close menu" }));
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("renders a theme toggle button", () => {
    renderHeader();

    expect(screen.getByRole("button", { name: "Switch to light theme" })).toBeInTheDocument();
  });

  it("applies data-theme to <html> only while mounted — never bleeds into Admin/Driver, which don't render Header", () => {
    expect(document.documentElement.getAttribute("data-theme")).toBeNull();

    const { unmount } = renderHeader();
    expect(document.documentElement.getAttribute("data-theme")).toBe("dark");

    unmount();
    expect(document.documentElement.getAttribute("data-theme")).toBeNull();
  });

  it("switching the toggle updates data-theme on <html> immediately", async () => {
    const user = userEvent.setup();
    renderHeader();

    expect(document.documentElement.getAttribute("data-theme")).toBe("dark");

    await user.click(screen.getByRole("button", { name: "Switch to light theme" }));

    expect(document.documentElement.getAttribute("data-theme")).toBe("light");
  });
});
