import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it } from "vitest";
import ThemeToggle from "./ThemeToggle";
import { ThemeProvider } from "../context/ThemeContext";

const STORAGE_KEY = "limousine-booking.theme";

function renderToggle() {
  return render(
    <ThemeProvider>
      <ThemeToggle />
    </ThemeProvider>
  );
}

afterEach(() => {
  localStorage.removeItem(STORAGE_KEY);
});

describe("ThemeToggle", () => {
  it("renders a single button offering to switch to light when dark is active", () => {
    renderToggle();

    expect(screen.getByRole("button", { name: "Switch to light theme" })).toBeInTheDocument();
  });

  it("switches to light and updates its label to offer switching back to dark", async () => {
    const user = userEvent.setup();
    renderToggle();

    await user.click(screen.getByRole("button", { name: "Switch to light theme" }));

    expect(screen.getByRole("button", { name: "Switch to dark theme" })).toBeInTheDocument();
  });

  it("clicking again switches back to dark", async () => {
    const user = userEvent.setup();
    renderToggle();

    await user.click(screen.getByRole("button", { name: "Switch to light theme" }));
    await user.click(screen.getByRole("button", { name: "Switch to dark theme" }));

    expect(screen.getByRole("button", { name: "Switch to light theme" })).toBeInTheDocument();
  });
});
