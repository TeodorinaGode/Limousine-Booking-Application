import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import SchedulePage from "./SchedulePage";
import * as driverBookingService from "../../services/driverBookingService";
import * as authContext from "../../context/AuthContext";
import type { DriverBookingListItemDto } from "../../types/driverBooking";
import type { PagedResult } from "../../types/api";

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

function pagedResult(items: DriverBookingListItemDto[]): PagedResult<DriverBookingListItemDto> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <SchedulePage />
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

describe("SchedulePage", () => {
  it("loads and displays the driver's own upcoming trips", async () => {
    mockedDriverBookingService.getMyBookings.mockResolvedValue(pagedResult([makeTrip()]));

    renderPage();

    expect(await screen.findByText("John", { exact: false })).toBeInTheDocument();
    expect(screen.getByText(/Zurich/)).toBeInTheDocument();
  });

  it("shows an empty state for a date range with no trips", async () => {
    mockedDriverBookingService.getMyBookings.mockResolvedValue(pagedResult([]));

    renderPage();

    expect(await screen.findByText("No Trips Found")).toBeInTheDocument();
  });

  it("reloads with the Today quick filter", async () => {
    mockedDriverBookingService.getMyBookings.mockResolvedValue(pagedResult([]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Trips Found");
    mockedDriverBookingService.getMyBookings.mockClear();

    await user.click(screen.getByRole("button", { name: "Today" }));

    await waitFor(() => {
      expect(mockedDriverBookingService.getMyBookings).toHaveBeenCalled();
    });
    const [params] = mockedDriverBookingService.getMyBookings.mock.calls[0];
    expect(params.dateFrom).toBe(params.dateTo);
  });
});
