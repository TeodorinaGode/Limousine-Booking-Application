import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import BookingDetailPage from "./BookingDetailPage";
import * as adminBookingService from "../../../services/adminBookingService";
import * as driverService from "../../../services/driverService";
import * as routeService from "../../../services/routeService";
import * as authContext from "../../../context/AuthContext";
import type { AdminBookingDetailDto } from "../../../types/adminBooking";
import type { DriverDto } from "../../../types/driver";
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
const mockedRouteService = vi.mocked(routeService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeBooking(overrides: Partial<AdminBookingDetailDto> = {}): AdminBookingDetailDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    bookingReference: "LM-20261225-123456",
    customerFirstName: "John",
    customerLastName: "Smith",
    customerEmail: "john.smith@example.com",
    customerPhone: "+41791234567",
    routeId: "22222222-2222-2222-2222-222222222222",
    route: { departureLocation: "Basel", destination: "Zurich" },
    bookingDate: "2026-12-25",
    pickupTime: "14:00:00",
    estimatedDurationMinutes: 90,
    estimatedEndTime: "15:30:00",
    pickupAddress: "Clarastrasse 10, Basel",
    passengerCount: 2,
    notes: null,
    price: 180,
    currency: "CHF",
    status: "Confirmed",
    rideStatus: "Upcoming",
    rideStatusHistory: [],
    driverId: "33333333-3333-3333-3333-333333333333",
    driverName: "John Driver",
    vehicleId: "44444444-4444-4444-4444-444444444444",
    vehicleDescription: "Mercedes-Benz V-Class - BS 123456",
    assignmentType: "Automatic",
    requiresManualAssignment: false,
    manualAssignmentReason: null,
    cancellationReason: null,
    cancelledAt: null,
    cancelledByEmail: null,
    createdAt: "2026-08-19T10:00:00Z",
    updatedAt: "2026-08-19T10:00:00Z",
    assignmentHistory: [],
    ...overrides,
  };
}

function makeDriver(overrides: Partial<DriverDto> = {}): DriverDto {
  return {
    id: "55555555-5555-5555-5555-555555555555",
    firstName: "Anna",
    lastName: "Driver",
    email: "anna.driver@example.com",
    phone: "+41791112233",
    isActive: true,
    isAvailable: true,
    vehicle: { id: "66666666-6666-6666-6666-666666666666", registrationNumber: "BS 999999", make: "Mercedes-Benz", model: "E-Class" },
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function pagedResult<T>(items: T[]): PagedResult<T> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
}

function renderPage(id = "11111111-1111-1111-1111-111111111111") {
  return render(
    <MemoryRouter initialEntries={[`/admin/bookings/${id}`]}>
      <Routes>
        <Route path="/admin/bookings/:id" element={<BookingDetailPage />} />
      </Routes>
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
  mockedRouteService.getRoutes.mockResolvedValue(pagedResult([]));
});

describe("BookingDetailPage", () => {
  it("renders full booking detail", async () => {
    mockedAdminBookingService.getBookingById.mockResolvedValue(makeBooking());

    renderPage();

    expect(await screen.findByText("LM-20261225-123456", { exact: false })).toBeInTheDocument();
    expect(screen.getByText("john.smith@example.com", { exact: false })).toBeInTheDocument();
    expect(screen.getByText(/Basel.*Zurich/)).toBeInTheDocument();
    expect(screen.getByText("John Driver", { exact: false })).toBeInTheDocument();
  });

  it("shows a manual-assignment banner when required", async () => {
    mockedAdminBookingService.getBookingById.mockResolvedValue(
      makeBooking({ status: "Pending", requiresManualAssignment: true, manualAssignmentReason: "No driver available.", driverId: null, driverName: null })
    );

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("No driver available.");
  });

  it("shows assignment history when present", async () => {
    mockedAdminBookingService.getBookingById.mockResolvedValue(
      makeBooking({
        assignmentHistory: [
          { driverName: "John Driver", vehicleDescription: "Mercedes V-Class", assignmentType: "Automatic", assignedByEmail: null, assignedAt: "2026-08-19T10:00:00Z" },
        ],
      })
    );

    renderPage();

    expect(await screen.findByText("Assignment History")).toBeInTheDocument();
    expect(screen.getByText("System (automatic)")).toBeInTheDocument();
  });

  it("edits the booking", async () => {
    mockedAdminBookingService.getBookingById.mockResolvedValue(makeBooking());
    mockedAdminBookingService.updateBooking.mockResolvedValue(makeBooking({ pickupAddress: "New Address 1, Basel" }));
    mockedRouteService.getRoutes.mockResolvedValue(
      pagedResult([{ id: "22222222-2222-2222-2222-222222222222", departureLocation: "Basel", destination: "Zurich", estimatedDurationMinutes: 90, price: 180, currency: "CHF", isActive: true, createdAt: "", updatedAt: "" }])
    );
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("LM-20261225-123456", { exact: false });

    await user.click(screen.getByRole("button", { name: "Edit" }));
    const addressInput = await screen.findByLabelText("Pickup address");
    await user.clear(addressInput);
    await user.type(addressInput, "New Address 1, Basel");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(mockedAdminBookingService.updateBooking).toHaveBeenCalledWith(
        "11111111-1111-1111-1111-111111111111",
        expect.objectContaining({ pickupAddress: "New Address 1, Basel" }),
        "test-token"
      );
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Booking updated successfully.");
  });

  it("assigns a driver via the assignment modal", async () => {
    mockedAdminBookingService.getBookingById.mockResolvedValue(makeBooking({ driverId: null, driverName: null, vehicleId: null, vehicleDescription: null, assignmentType: null }));
    const driver = makeDriver();
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([driver]));
    mockedAdminBookingService.assignDriver.mockResolvedValue(makeBooking({ driverName: driver.firstName + " " + driver.lastName }));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("LM-20261225-123456", { exact: false });

    await user.click(screen.getByRole("button", { name: "Assign Driver" }));
    await user.selectOptions(await screen.findByLabelText("Driver"), driver.id);
    await user.click(screen.getByRole("button", { name: "Assign" }));

    await waitFor(() => {
      expect(mockedAdminBookingService.assignDriver).toHaveBeenCalledWith(
        "11111111-1111-1111-1111-111111111111",
        { driverId: driver.id, vehicleId: driver.vehicle!.id },
        "test-token"
      );
    });
  });

  it("cancels the booking after confirmation", async () => {
    mockedAdminBookingService.getBookingById.mockResolvedValue(makeBooking());
    mockedAdminBookingService.cancelBooking.mockResolvedValue(makeBooking({ status: "Cancelled" }));
    vi.spyOn(window, "confirm").mockReturnValue(true);
    vi.spyOn(window, "prompt").mockReturnValue("Customer requested cancellation");
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("LM-20261225-123456", { exact: false });

    await user.click(screen.getByRole("button", { name: "Cancel Booking" }));

    await waitFor(() => {
      expect(mockedAdminBookingService.cancelBooking).toHaveBeenCalledWith(
        "11111111-1111-1111-1111-111111111111",
        { reason: "Customer requested cancellation" },
        "test-token"
      );
    });
  });

  it("does not cancel when the confirmation is declined", async () => {
    mockedAdminBookingService.getBookingById.mockResolvedValue(makeBooking());
    vi.spyOn(window, "confirm").mockReturnValue(false);
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("LM-20261225-123456", { exact: false });

    await user.click(screen.getByRole("button", { name: "Cancel Booking" }));

    expect(mockedAdminBookingService.cancelBooking).not.toHaveBeenCalled();
  });

  it("hides edit/assign/cancel actions for a cancelled booking", async () => {
    mockedAdminBookingService.getBookingById.mockResolvedValue(makeBooking({ status: "Cancelled" }));

    renderPage();
    await screen.findByText("LM-20261225-123456", { exact: false });

    expect(screen.queryByRole("button", { name: "Edit" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Cancel Booking" })).not.toBeInTheDocument();
  });

  it("resends the confirmation email for a confirmed booking", async () => {
    mockedAdminBookingService.getBookingById.mockResolvedValue(makeBooking({ status: "Confirmed" }));
    mockedAdminBookingService.resendConfirmation.mockResolvedValue(makeBooking({ status: "Confirmed" }));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("LM-20261225-123456", { exact: false });

    await user.click(screen.getByRole("button", { name: "Resend Confirmation Email" }));

    await waitFor(() => {
      expect(mockedAdminBookingService.resendConfirmation).toHaveBeenCalledWith("11111111-1111-1111-1111-111111111111", "test-token");
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Confirmation email queued for resend.");
  });

  it("does not show the resend button for a pending booking", async () => {
    mockedAdminBookingService.getBookingById.mockResolvedValue(makeBooking({ status: "Pending", requiresManualAssignment: true }));

    renderPage();
    await screen.findByText("LM-20261225-123456", { exact: false });

    expect(screen.queryByRole("button", { name: "Resend Confirmation Email" })).not.toBeInTheDocument();
  });
});
