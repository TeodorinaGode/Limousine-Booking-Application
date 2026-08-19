import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import DriverDetailsPage from "./DriverDetailsPage";
import * as driverService from "../../../services/driverService";
import * as availabilityService from "../../../services/availabilityService";
import * as authContext from "../../../context/AuthContext";
import type { DriverDto } from "../../../types/driver";

vi.mock("../../../services/driverService");
vi.mock("../../../services/availabilityService");
vi.mock("../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedDriverService = vi.mocked(driverService);
const mockedAvailabilityService = vi.mocked(availabilityService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeDriver(overrides: Partial<DriverDto> = {}): DriverDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    firstName: "John",
    lastName: "Smith",
    email: "john.smith@example.com",
    phone: "+41791234567",
    isActive: true,
    isAvailable: true,
    vehicle: null,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
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

function renderWithRoute(driverId: string) {
  return render(
    <MemoryRouter initialEntries={[`/admin/drivers/${driverId}`]}>
      <Routes>
        <Route path="/admin/drivers/:id" element={<DriverDetailsPage />} />
      </Routes>
    </MemoryRouter>
  );
}

describe("DriverDetailsPage", () => {
  it("shows the driver's personal/work info and availability schedule", async () => {
    const driver = makeDriver();
    mockedDriverService.getDriverById.mockResolvedValue(driver);
    mockedAvailabilityService.getDriverSchedule.mockResolvedValue({
      isCurrentlyAvailable: true,
      schedule: [
        {
          id: "avail-1",
          driverId: driver.id,
          date: "2026-09-15",
          startTime: "08:00:00",
          endTime: "17:00:00",
          isAvailable: true,
          notes: null,
          createdAt: "2026-01-01T00:00:00Z",
          updatedAt: "2026-01-01T00:00:00Z",
        },
      ],
    });

    renderWithRoute(driver.id);

    expect(await screen.findByText(/john\.smith@example\.com/)).toBeInTheDocument();
    expect(screen.getByText("08:00")).toBeInTheDocument();
    expect(screen.getByText("17:00")).toBeInTheDocument();
    expect(mockedAvailabilityService.getDriverSchedule).toHaveBeenCalledWith(
      driver.id,
      expect.objectContaining({}),
      "test-token"
    );
  });

  it("shows an error message when the driver fails to load", async () => {
    mockedDriverService.getDriverById.mockRejectedValue(new Error("Driver not found."));
    mockedAvailabilityService.getDriverSchedule.mockResolvedValue({ isCurrentlyAvailable: false, schedule: [] });

    renderWithRoute("11111111-1111-1111-1111-111111111111");

    expect(await screen.findByRole("alert")).toHaveTextContent("Driver not found.");
  });
});
