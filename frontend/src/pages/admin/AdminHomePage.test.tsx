import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import AdminHomePage from "./AdminHomePage";
import * as adminBookingService from "../../services/adminBookingService";
import * as reportService from "../../services/reportService";
import * as driverService from "../../services/driverService";
import * as vehicleService from "../../services/vehicleService";
import * as authContext from "../../context/AuthContext";
import type { AdminDashboardDto } from "../../types/adminBooking";
import type { ReportSummaryDto } from "../../types/reports";

vi.mock("../../services/adminBookingService");
vi.mock("../../services/reportService");
vi.mock("../../services/driverService");
vi.mock("../../services/vehicleService");
vi.mock("../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedAdminBookingService = vi.mocked(adminBookingService);
const mockedReportService = vi.mocked(reportService);
const mockedDriverService = vi.mocked(driverService);
const mockedVehicleService = vi.mocked(vehicleService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeSummary(overrides: Partial<ReportSummaryDto> = {}): ReportSummaryDto {
  return {
    dateFrom: "2026-12-01",
    dateTo: "2026-12-25",
    totalBookings: 42,
    confirmedBookings: 30,
    pendingBookings: 5,
    completedBookings: 8,
    cancelledBookings: 5,
    grossRevenue: 18500,
    completedRevenue: 13200,
    averageBookingValue: 148,
    averageCompletedBookingValue: 183,
    manualAssignments: 14,
    automaticAssignments: 111,
    currency: "CHF",
    ...overrides,
  };
}

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
    user: { id: "u1", email: "admin@example.com", firstName: "Admin", lastName: "User", role: "Administrator", languageCode: null },
    accessToken: "test-token",
    expiresAt: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
  });
  mockedReportService.getSummary.mockResolvedValue(makeSummary());
  mockedReportService.getBookingsByDay.mockResolvedValue([]);
  mockedDriverService.getDrivers.mockResolvedValue({ items: [], page: 1, pageSize: 1, totalCount: 6, totalPages: 1 });
  mockedVehicleService.getVehicles.mockResolvedValue({ items: [], page: 1, pageSize: 1, totalCount: 4, totalPages: 1 });
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

  it("shows this month's completed count and revenue from the reports summary API", async () => {
    mockedAdminBookingService.getDashboard.mockResolvedValue(makeDashboard({ upcomingTripsCount: 3 }));
    mockedReportService.getSummary.mockResolvedValue(makeSummary({ completedBookings: 8, grossRevenue: 18500 }));

    renderPage();

    const completedLabel = await screen.findByText("Completed This Month");
    expect(completedLabel.closest(".metric-card")).toHaveTextContent("8");
    expect(screen.getByText("Revenue This Month").closest(".metric-card")).toHaveTextContent("CHF 18500.00");
  });

  it("shows available drivers and active vehicles counts, sourced from the existing driver/vehicle endpoints", async () => {
    mockedAdminBookingService.getDashboard.mockResolvedValue(makeDashboard());
    mockedDriverService.getDrivers.mockResolvedValue({ items: [], page: 1, pageSize: 1, totalCount: 6, totalPages: 1 });
    mockedVehicleService.getVehicles.mockResolvedValue({ items: [], page: 1, pageSize: 1, totalCount: 4, totalPages: 1 });

    renderPage();

    const driversLabel = await screen.findByText("Available Drivers");
    expect(driversLabel.closest(".metric-card")).toHaveTextContent("6");
    expect(screen.getByText("Active Vehicles").closest(".metric-card")).toHaveTextContent("4");
    expect(mockedDriverService.getDrivers).toHaveBeenCalledWith(
      expect.objectContaining({ isActive: true, isAvailable: true }),
      "test-token"
    );
    expect(mockedVehicleService.getVehicles).toHaveBeenCalledWith(
      expect.objectContaining({ isActive: true }),
      "test-token"
    );
  });

  it("links to the reports page", async () => {
    mockedAdminBookingService.getDashboard.mockResolvedValue(makeDashboard());

    renderPage();

    expect(await screen.findByRole("link", { name: "Reports" })).toHaveAttribute("href", "/admin/reports");
  });
});
