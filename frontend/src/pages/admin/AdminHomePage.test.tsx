import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import AdminHomePage from "./AdminHomePage";
import * as adminBookingService from "../../services/adminBookingService";
import * as authContext from "../../context/AuthContext";
import type { AdminDashboardDto } from "../../types/adminBooking";

vi.mock("../../services/adminBookingService");
vi.mock("../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedAdminBookingService = vi.mocked(adminBookingService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeDashboard(overrides: Partial<AdminDashboardDto> = {}): AdminDashboardDto {
  return {
    totalBookings: 42,
    todaysBookings: 3,
    pendingBookings: 5,
    requiresManualAssignmentCount: 2,
    confirmedBookings: 30,
    cancelledBookings: 5,
    upcomingTripsCount: 8,
    upcomingBookings: [],
    notifications: { pending: 1, retrying: 2, failed: 3, sentToday: 10 },
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <AdminHomePage />
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
});

describe("AdminHomePage", () => {
  it("shows the notification summary from the dashboard", async () => {
    mockedAdminBookingService.getDashboard.mockResolvedValue(makeDashboard());

    renderPage();

    expect(await screen.findByText("Notifications")).toBeInTheDocument();
    expect(screen.getByText("Failed: 3")).toBeInTheDocument();
    expect(screen.getByText("Retrying: 2")).toBeInTheDocument();
    expect(screen.getByText("Sent today: 10")).toBeInTheDocument();
  });

  it("shows an error when the dashboard fails to load", async () => {
    mockedAdminBookingService.getDashboard.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("lists upcoming bookings with a link to the booking detail page", async () => {
    mockedAdminBookingService.getDashboard.mockResolvedValue(
      makeDashboard({
        upcomingBookings: [
          {
            id: "11111111-1111-1111-1111-111111111111",
            bookingReference: "LM-20261225-123456",
            bookingDate: "2026-12-25",
            pickupTime: "14:00:00",
            route: { departureLocation: "Basel", destination: "Zurich" },
            customerFirstName: "John",
            customerLastName: "Smith",
            driverName: "Dev Driver",
            vehicleDescription: "Mercedes-Benz E-Class - BS 999001",
            status: "Confirmed",
          },
        ],
      })
    );

    renderPage();

    expect(await screen.findByText("John Smith")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /14:00/ })).toHaveAttribute("href", "/admin/bookings/11111111-1111-1111-1111-111111111111");
  });
});
