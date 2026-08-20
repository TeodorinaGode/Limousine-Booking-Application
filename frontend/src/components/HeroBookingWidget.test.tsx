import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { describe, expect, it } from "vitest";
import HeroBookingWidget from "./HeroBookingWidget";
import type { PublicRouteDto } from "../types/booking";

const routes: PublicRouteDto[] = [
  { id: "route-1", departureLocation: "Basel", destination: "Zurich", estimatedDurationMinutes: 60, price: 180, currency: "CHF" },
  { id: "route-2", departureLocation: "Basel", destination: "Bern", estimatedDurationMinutes: 75, price: 210, currency: "CHF" },
];

let capturedLocationState: unknown = null;

function RouteStateCapture() {
  const location = useLocation();
  capturedLocationState = location.state;
  return <p data-testid="probe">probed</p>;
}

function renderWidget() {
  capturedLocationState = null;
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <Routes>
        <Route path="/" element={<HeroBookingWidget routes={routes} />} />
        <Route path="/booking" element={<RouteStateCapture />} />
      </Routes>
    </MemoryRouter>
  );
}

describe("HeroBookingWidget", () => {
  it("populates the From dropdown with distinct departure locations", () => {
    renderWidget();

    expect(screen.getByRole("option", { name: "Basel" })).toBeInTheDocument();
  });

  it("scopes the To dropdown to routes departing from the selected origin", async () => {
    const user = userEvent.setup();
    renderWidget();

    await user.selectOptions(screen.getByLabelText("From"), "Basel");

    expect(screen.getByRole("option", { name: "Zurich" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "Bern" })).toBeInTheDocument();
  });

  it("navigates to the booking flow with the matched route id and entered details", async () => {
    const user = userEvent.setup();
    renderWidget();

    await user.selectOptions(screen.getByLabelText("From"), "Basel");
    await user.selectOptions(screen.getByLabelText("To"), "Zurich");
    await user.clear(screen.getByLabelText("Passengers"));
    await user.type(screen.getByLabelText("Passengers"), "3");
    await user.click(screen.getByRole("button", { name: "Check Availability" }));

    expect(await screen.findByTestId("probe")).toBeInTheDocument();
    expect(capturedLocationState).toMatchObject({ routeId: "route-1", passengerCount: 3 });
  });

  it("still navigates to booking even when no matching route is selected", async () => {
    const user = userEvent.setup();
    renderWidget();

    await user.click(screen.getByRole("button", { name: "Check Availability" }));

    expect(await screen.findByTestId("probe")).toBeInTheDocument();
    expect(capturedLocationState).toMatchObject({ routeId: undefined });
  });
});
