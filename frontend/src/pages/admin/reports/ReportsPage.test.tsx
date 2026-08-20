import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ReportsPage from "./ReportsPage";
import * as reportService from "../../../services/reportService";
import * as adminBookingService from "../../../services/adminBookingService";
import * as authContext from "../../../context/AuthContext";
import type {
  AssignmentReportDto,
  BookingStatusDistributionDto,
  BookingsByDayDto,
  CancellationReportDto,
  DriverActivityDto,
  PassengerReportDto,
  PopularRouteDto,
  ReportSummaryDto,
  RevenueByDayDto,
  UnassignedBookingDto,
  UpcomingOperationDto,
  VehicleUsageDto,
} from "../../../types/reports";

vi.mock("../../../services/reportService");
vi.mock("../../../services/adminBookingService");
vi.mock("../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedReportService = vi.mocked(reportService);
const mockedAdminBookingService = vi.mocked(adminBookingService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeSummary(overrides: Partial<ReportSummaryDto> = {}): ReportSummaryDto {
  return {
    dateFrom: "2026-09-01",
    dateTo: "2026-09-15",
    totalBookings: 125,
    confirmedBookings: 95,
    pendingBookings: 8,
    completedBookings: 72,
    cancelledBookings: 22,
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

function renderPage() {
  return render(
    <MemoryRouter>
      <ReportsPage />
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

  mockedReportService.getSummary.mockResolvedValue(makeSummary());
  mockedReportService.getRevenueByDay.mockResolvedValue([] as RevenueByDayDto[]);
  mockedReportService.getBookingsByDay.mockResolvedValue([] as BookingsByDayDto[]);
  mockedReportService.getPopularRoutes.mockResolvedValue([] as PopularRouteDto[]);
  mockedReportService.getDriverActivity.mockResolvedValue([] as DriverActivityDto[]);
  mockedReportService.getVehicleUsage.mockResolvedValue([] as VehicleUsageDto[]);
  mockedReportService.getPassengerReport.mockResolvedValue({
    dateFrom: "2026-09-01",
    dateTo: "2026-09-15",
    totalPassengers: 312,
    averagePassengersPerBooking: 2.5,
    maximumPassengersInABooking: 7,
  } as PassengerReportDto);
  mockedReportService.getStatusDistribution.mockResolvedValue([] as BookingStatusDistributionDto[]);
  mockedReportService.getAssignmentReport.mockResolvedValue({
    dateFrom: "2026-09-01",
    dateTo: "2026-09-15",
    automaticAssignments: 111,
    manualAssignments: 14,
    requiresManualAssignment: 2,
    manualAssignmentRate: 11.2,
    assignmentSuccessRate: 96.2,
  } as AssignmentReportDto);
  mockedReportService.getCancellationReport.mockResolvedValue({
    dateFrom: "2026-09-01",
    dateTo: "2026-09-15",
    totalCancellations: 0,
    totalBookings: 0,
    cancellationRate: 0,
    cancellationsByRoute: [],
    cancellationsByDay: [],
    cancellationsByReason: [],
  } as CancellationReportDto);
  mockedReportService.getUnassignedBookings.mockResolvedValue([] as UnassignedBookingDto[]);
  mockedReportService.getUpcomingOperations.mockResolvedValue([] as UpcomingOperationDto[]);
  mockedAdminBookingService.getBookings.mockResolvedValue({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 });
});

describe("ReportsPage", () => {
  it("loads and displays the summary metrics", async () => {
    renderPage();

    expect(await screen.findByText("125")).toBeInTheDocument();
    expect(screen.getByText("CHF 18500.00")).toBeInTheDocument();
    expect(screen.getByText("CHF 13200.00")).toBeInTheDocument();
  });

  it("shows an error when reports fail to load", async () => {
    mockedReportService.getSummary.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("shows empty states for unassigned and upcoming operations", async () => {
    renderPage();

    expect(await screen.findByText("No bookings currently require manual assignment.")).toBeInTheDocument();
    expect(await screen.findByText("No upcoming trips for the selected period.")).toBeInTheDocument();
  });

  it("shows unassigned bookings with a link to the booking detail page", async () => {
    mockedReportService.getUnassignedBookings.mockResolvedValue([
      {
        id: "11111111-1111-1111-1111-111111111111",
        bookingReference: "LM-20261225-123456",
        bookingDate: "2026-12-25",
        pickupTime: "14:00:00",
        route: { departureLocation: "Basel", destination: "Zurich" },
        customerFirstName: "John",
        customerLastName: "Smith",
        passengerCount: 2,
        reason: "No eligible driver found.",
        createdAt: "2026-12-20T10:00:00Z",
      },
    ]);

    renderPage();

    expect(await screen.findByText("No eligible driver found.")).toBeInTheDocument();
    const links = await screen.findAllByRole("link", { name: "View" });
    expect(links[0]).toHaveAttribute("href", "/admin/bookings/11111111-1111-1111-1111-111111111111");
  });

  it("reloads reports when the date range changes", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByText("125");
    mockedReportService.getSummary.mockClear();

    await user.click(screen.getByRole("button", { name: "Today" }));

    await waitFor(() => {
      expect(mockedReportService.getSummary).toHaveBeenCalled();
    });
    const [params] = mockedReportService.getSummary.mock.calls[0];
    expect(params.dateFrom).toBe(params.dateTo);
  });

  it("changes the upcoming-operations period without refetching the main reports", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByText("125");
    mockedReportService.getUpcomingOperations.mockClear();

    await user.selectOptions(screen.getByLabelText("Upcoming operations:"), "next30");

    await waitFor(() => {
      expect(mockedReportService.getUpcomingOperations).toHaveBeenCalledWith("next30", "test-token");
    });
  });

  it("exports the bookings report as CSV", async () => {
    mockedAdminBookingService.getBookings.mockResolvedValue({
      items: [
        {
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
          rideStatus: "Upcoming",
          driverName: "Dev Driver",
          vehicleDescription: "Mercedes-Benz E-Class - BS 999001",
          assignment: "Automatic",
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    });
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("LM-20261225-123456");

    const exportButtons = screen.getAllByRole("button", { name: "Export CSV" });
    await user.click(exportButtons[exportButtons.length - 1]);

    await waitFor(() => {
      expect(mockedReportService.exportBookingsCsv).toHaveBeenCalledWith(
        expect.objectContaining({ dateFrom: expect.any(String), dateTo: expect.any(String) }),
        "test-token"
      );
    });
  });
});
