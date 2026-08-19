import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import BookingsPage from "./BookingsPage";
import * as adminBookingService from "../../../services/adminBookingService";
import * as driverService from "../../../services/driverService";
import * as vehicleService from "../../../services/vehicleService";
import * as routeService from "../../../services/routeService";
import * as authContext from "../../../context/AuthContext";
import type { AdminBookingListItemDto } from "../../../types/adminBooking";
import type { PagedResult } from "../../../types/api";

vi.mock("../../../services/adminBookingService");
vi.mock("../../../services/driverService");
vi.mock("../../../services/vehicleService");
vi.mock("../../../services/routeService");
vi.mock("../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedAdminBookingService = vi.mocked(adminBookingService);
const mockedDriverService = vi.mocked(driverService);
const mockedVehicleService = vi.mocked(vehicleService);
const mockedRouteService = vi.mocked(routeService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeBooking(overrides: Partial<AdminBookingListItemDto> = {}): AdminBookingListItemDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    bookingReference: "LM-20261225-123456",
    customerFirstName: "John",
    customerLastName: "Smith",
    route: { departureLocation: "Basel", destination: "Zurich" },
    bookingDate: "2026-12-25",
    pickupTime: "14:00:00",
    passengerCount: 2,
    price: 180,
    currency: "CHF",
    status: "Confirmed",
    driverName: "Dev Driver",
    vehicleDescription: "Mercedes-Benz V-Class - BS 123456",
    assignment: "Automatic",
    ...overrides,
  };
}

function pagedResult<T>(items: T[]): PagedResult<T> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <BookingsPage />
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  mockedUseAuth.mockReturnValue({
    user: { id: "u1", email: "admin@example.com", firstName: "Admin", lastName: "User", role: "Administrator" },
    accessToken: "test-token",
    expiresAt: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
  });
  mockedDriverService.getDrivers.mockResolvedValue(pagedResult([]));
  mockedVehicleService.getVehicles.mockResolvedValue(pagedResult([]));
  mockedRouteService.getRoutes.mockResolvedValue(pagedResult([]));
});

describe("BookingsPage", () => {
  it("renders the booking list", async () => {
    mockedAdminBookingService.getBookings.mockResolvedValue(pagedResult([makeBooking()]));

    renderPage();

    expect(await screen.findByText("LM-20261225-123456")).toBeInTheDocument();
    expect(screen.getByText("John Smith")).toBeInTheDocument();
  });

  it("shows an error message when loading fails", async () => {
    mockedAdminBookingService.getBookings.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("defaults to the Active status filter (Pending + Confirmed)", async () => {
    mockedAdminBookingService.getBookings.mockResolvedValue(pagedResult([]));

    renderPage();

    await waitFor(() => {
      expect(mockedAdminBookingService.getBookings).toHaveBeenCalledWith(
        expect.objectContaining({ status: "Pending,Confirmed" }),
        "test-token"
      );
    });
  });

  it("searches bookings via the backend, not client-side filtering", async () => {
    mockedAdminBookingService.getBookings.mockResolvedValue(pagedResult([makeBooking()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("LM-20261225-123456");

    await user.type(screen.getByLabelText("Search bookings"), "John");

    await waitFor(
      () => {
        expect(mockedAdminBookingService.getBookings).toHaveBeenLastCalledWith(
          expect.objectContaining({ search: "John" }),
          "test-token"
        );
      },
      { timeout: 1000 }
    );
  });

  it("filters by status", async () => {
    mockedAdminBookingService.getBookings.mockResolvedValue(pagedResult([]));
    const user = userEvent.setup();

    renderPage();
    await waitFor(() => expect(mockedAdminBookingService.getBookings).toHaveBeenCalled());

    await user.selectOptions(screen.getByLabelText("Status:"), "Pending");

    await waitFor(() => {
      expect(mockedAdminBookingService.getBookings).toHaveBeenLastCalledWith(
        expect.objectContaining({ status: "Pending" }),
        "test-token"
      );
    });
  });

  it("filters by assignment status", async () => {
    mockedAdminBookingService.getBookings.mockResolvedValue(pagedResult([]));
    const user = userEvent.setup();

    renderPage();
    await waitFor(() => expect(mockedAdminBookingService.getBookings).toHaveBeenCalled());

    await user.selectOptions(screen.getByLabelText("Assignment:"), "requiresManual");

    await waitFor(() => {
      expect(mockedAdminBookingService.getBookings).toHaveBeenLastCalledWith(
        expect.objectContaining({ assignmentFilter: "requiresManual" }),
        "test-token"
      );
    });
  });

  it("links to the booking detail page", async () => {
    mockedAdminBookingService.getBookings.mockResolvedValue(pagedResult([makeBooking()]));

    renderPage();
    await screen.findByText("LM-20261225-123456");

    expect(screen.getByRole("link", { name: "View" })).toHaveAttribute(
      "href",
      "/admin/bookings/11111111-1111-1111-1111-111111111111"
    );
  });
});
