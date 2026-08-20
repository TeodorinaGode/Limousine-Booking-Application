import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import TripDetailPage from "./TripDetailPage";
import * as driverBookingService from "../../services/driverBookingService";
import * as authContext from "../../context/AuthContext";
import type { DriverBookingDetailDto } from "../../types/driverBooking";

vi.mock("../../services/driverBookingService");
vi.mock("../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedDriverBookingService = vi.mocked(driverBookingService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeTrip(overrides: Partial<DriverBookingDetailDto> = {}): DriverBookingDetailDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    bookingReference: "LM-20261225-123456",
    customerFirstName: "John",
    customerLastName: "Smith",
    customerPhone: "+41791234567",
    route: { departureLocation: "Basel", destination: "Zurich" },
    bookingDate: "2026-12-25",
    pickupTime: "14:00:00",
    estimatedDurationMinutes: 90,
    estimatedEndTime: "15:30:00",
    pickupAddress: "Bahnhofplatz 1, Basel",
    passengerCount: 2,
    notes: null,
    status: "Confirmed",
    rideStatus: "Upcoming",
    rideStatusHistory: [],
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/driver/bookings/11111111-1111-1111-1111-111111111111"]}>
      <Routes>
        <Route path="/driver/bookings/:id" element={<TripDetailPage />} />
      </Routes>
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  mockedUseAuth.mockReturnValue({
    user: { id: "u1", email: "driver@example.com", firstName: "John", lastName: "Driver", role: "Driver" },
    accessToken: "test-token",
    expiresAt: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
  });
});

describe("TripDetailPage", () => {
  it("shows the Start Ride action for an Upcoming trip", async () => {
    mockedDriverBookingService.getMyBookingById.mockResolvedValue(makeTrip());

    renderPage();

    expect(await screen.findByRole("button", { name: "Start Ride" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Mark Passenger Picked Up" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Complete Ride" })).not.toBeInTheDocument();
  });

  it("starts the ride and shows the next available action", async () => {
    mockedDriverBookingService.getMyBookingById.mockResolvedValue(makeTrip());
    mockedDriverBookingService.startRide.mockResolvedValue(makeTrip({ rideStatus: "OnTheWay" }));
    const user = userEvent.setup();

    renderPage();
    await user.click(await screen.findByRole("button", { name: "Start Ride" }));

    await waitFor(() => {
      expect(mockedDriverBookingService.startRide).toHaveBeenCalledWith(
        "11111111-1111-1111-1111-111111111111",
        "test-token"
      );
    });
    expect(await screen.findByRole("button", { name: "Mark Passenger Picked Up" })).toBeInTheDocument();
    expect(await screen.findByRole("status")).toHaveTextContent("Ride started.");
  });

  it("shows no ride-status action once the ride is completed", async () => {
    mockedDriverBookingService.getMyBookingById.mockResolvedValue(makeTrip({ rideStatus: "Completed", status: "Completed" }));

    renderPage();

    await screen.findByText("Trip LM-20261225-123456");
    expect(screen.queryByRole("button", { name: "Start Ride" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Mark Passenger Picked Up" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Complete Ride" })).not.toBeInTheDocument();
  });

  it("shows the backend's conflict error when a double action is attempted", async () => {
    mockedDriverBookingService.getMyBookingById.mockResolvedValue(makeTrip({ rideStatus: "OnTheWay" }));
    mockedDriverBookingService.markPassengerPickedUp.mockRejectedValue(new Error("The passenger has already been picked up."));
    const user = userEvent.setup();

    renderPage();
    await user.click(await screen.findByRole("button", { name: "Mark Passenger Picked Up" }));

    expect(await screen.findByText("The passenger has already been picked up.")).toBeInTheDocument();
  });
});
