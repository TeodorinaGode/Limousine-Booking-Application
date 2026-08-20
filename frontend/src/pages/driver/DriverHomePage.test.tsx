import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import DriverHomePage from "./DriverHomePage";
import * as driverBookingService from "../../services/driverBookingService";
import * as authContext from "../../context/AuthContext";
import type { DriverBookingListItemDto, DriverDashboardDto } from "../../types/driverBooking";

vi.mock("../../services/driverBookingService");
vi.mock("../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedDriverBookingService = vi.mocked(driverBookingService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeTrip(overrides: Partial<DriverBookingListItemDto> = {}): DriverBookingListItemDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    bookingReference: "LM-20261225-123456",
    route: { departureLocation: "Basel", destination: "Zurich" },
    bookingDate: "2026-12-25",
    pickupTime: "14:00:00",
    pickupAddress: "Bahnhofplatz 1, Basel",
    passengerCount: 2,
    customerFirstName: "John",
    customerLastName: "Smith",
    status: "Confirmed",
    rideStatus: "Upcoming",
    ...overrides,
  };
}

function makeDashboard(overrides: Partial<DriverDashboardDto> = {}): DriverDashboardDto {
  return {
    today: "2026-12-25",
    isAvailable: true,
    todaysTripCount: 1,
    completedTodayCount: 0,
    upcomingTripCount: 4,
    todaysTrips: [makeTrip()],
    nextTrip: makeTrip(),
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <DriverHomePage />
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  mockedUseAuth.mockReturnValue({
    user: { id: "u1", email: "driver@example.com", firstName: "John", lastName: "Driver", role: "Driver", languageCode: null },
    accessToken: "test-token",
    expiresAt: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
  });
});

describe("DriverHomePage", () => {
  it("shows today's counters and the next trip", async () => {
    mockedDriverBookingService.getMyDashboard.mockResolvedValue(makeDashboard());

    renderPage();

    expect(await screen.findByText("Today's trips: 1")).toBeInTheDocument();
    expect(screen.getByText("Completed today: 0")).toBeInTheDocument();
    expect(screen.getByText("Upcoming trips: 4")).toBeInTheDocument();
    expect(screen.getByText("Currently: Available")).toBeInTheDocument();
    expect(screen.getByText("Next Trip")).toBeInTheDocument();
  });

  it("shows an empty state when there are no trips today", async () => {
    mockedDriverBookingService.getMyDashboard.mockResolvedValue(
      makeDashboard({ todaysTrips: [], nextTrip: null, todaysTripCount: 0 })
    );

    renderPage();

    expect(await screen.findByText("No Upcoming Trips")).toBeInTheDocument();
    expect(screen.queryByText("Next Trip")).not.toBeInTheDocument();
  });

  it("shows an error when the dashboard fails to load", async () => {
    mockedDriverBookingService.getMyDashboard.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });
});
