import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import TravelDateTimeField from "./TravelDateTimeField";

// The calendar chunk's dynamic import() can occasionally exceed the default
// findBy* timeout under a heavily parallel full-suite run (not in isolation)
// — same mitigation already applied to BookingPage.test.tsx.
vi.setConfig({ testTimeout: 15000 });

describe("TravelDateTimeField", () => {
  it("shows placeholders when no date/time is set", () => {
    render(<TravelDateTimeField date="" time="" onApply={vi.fn()} />);

    expect(screen.getByRole("button", { name: "Date" })).toHaveTextContent("Select a date");
    expect(screen.getByRole("button", { name: "Time" })).toHaveTextContent("Select a time");
  });

  it("shows the formatted date and time when both are set", () => {
    render(<TravelDateTimeField date="2026-06-20" time="14:30" onApply={vi.fn()} />);

    expect(screen.getByRole("button", { name: "Date" })).toHaveTextContent("20");
    expect(screen.getByRole("button", { name: "Time" })).toHaveTextContent("14:30");
  });

  it("opens the picker modal when the date trigger is clicked", async () => {
    const user = userEvent.setup();
    render(<TravelDateTimeField date="2026-06-20" time="" onApply={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Date" }));

    expect(screen.getByRole("dialog", { name: "Date of travel" })).toBeInTheDocument();
  });

  it("applies the selected date and time, then closes", async () => {
    const onApply = vi.fn();
    const user = userEvent.setup();
    render(<TravelDateTimeField date="2026-06-20" time="" onApply={onApply} />);

    await user.click(screen.getByRole("button", { name: "Date" }));
    const dialog = screen.getByRole("dialog", { name: "Date of travel" });

    await user.click(await within(dialog).findByRole("button", { name: /June 18th, 2026/ }, { timeout: 5000 }));
    const hourCol = within(dialog).getByRole("group", { name: "Hour" });
    const minuteCol = within(dialog).getByRole("group", { name: "Minute" });
    await user.click(within(hourCol).getByRole("button", { name: "09" }));
    await user.click(within(minuteCol).getByRole("button", { name: "15" }));
    await user.click(within(dialog).getByRole("button", { name: "Apply" }));

    expect(onApply).toHaveBeenCalledWith("2026-06-18", "09:15");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("discards changes on cancel", async () => {
    const onApply = vi.fn();
    const user = userEvent.setup();
    render(<TravelDateTimeField date="2026-06-20" time="10:00" onApply={onApply} />);

    await user.click(screen.getByRole("button", { name: "Date" }));
    const dialog = screen.getByRole("dialog", { name: "Date of travel" });
    await user.click(await within(dialog).findByRole("button", { name: /June 25th, 2026/ }, { timeout: 5000 }));
    await user.click(within(dialog).getByRole("button", { name: "Cancel" }));

    expect(onApply).not.toHaveBeenCalled();
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Date" })).toHaveTextContent("20");
  });

  it("disables dates before minDate", async () => {
    const user = userEvent.setup();
    render(<TravelDateTimeField date="2026-06-20" time="" minDate={new Date(2026, 5, 15)} onApply={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Date" }));
    const dialog = screen.getByRole("dialog", { name: "Date of travel" });

    expect(await within(dialog).findByRole("button", { name: /June 10th, 2026/ }, { timeout: 5000 })).toBeDisabled();
    expect(within(dialog).getByRole("button", { name: /June 20th, 2026/ })).toBeEnabled();
  });
});
