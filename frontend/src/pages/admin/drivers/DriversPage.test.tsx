import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import DriversPage from "./DriversPage";
import * as driverService from "../../../services/driverService";
import * as vehicleService from "../../../services/vehicleService";
import * as authContext from "../../../context/AuthContext";
import type { DriverDto } from "../../../types/driver";
import type { VehicleDto } from "../../../types/vehicle";
import type { PagedResult } from "../../../types/api";

vi.mock("../../../services/driverService");
vi.mock("../../../services/vehicleService");
vi.mock("../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedDriverService = vi.mocked(driverService);
const mockedVehicleService = vi.mocked(vehicleService);
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

function makeVehicle(overrides: Partial<VehicleDto> = {}): VehicleDto {
  return {
    id: "22222222-2222-2222-2222-222222222222",
    registrationNumber: "BS 123456",
    make: "Mercedes-Benz",
    model: "V-Class",
    vehicleType: "Van",
    passengerCapacity: 7,
    isActive: true,
    notes: null,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function pagedResult<T>(items: T[]): PagedResult<T> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <DriversPage />
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
  mockedVehicleService.getVehicles.mockResolvedValue(pagedResult([makeVehicle()]));
});

describe("DriversPage", () => {
  it("renders the driver list", async () => {
    mockedDriverService.getDrivers.mockResolvedValue(
      pagedResult([makeDriver(), makeDriver({ id: "2", firstName: "Mark", lastName: "Brown", email: "mark@example.com" })])
    );

    renderPage();

    expect(await screen.findByText("John Smith")).toBeInTheDocument();
    expect(screen.getByText("Mark Brown")).toBeInTheDocument();
  });

  it("shows an error message when loading fails", async () => {
    mockedDriverService.getDrivers.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("searches drivers via the backend, not client-side filtering", async () => {
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([makeDriver()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("John Smith");

    await user.type(screen.getByLabelText("Search drivers"), "John");

    await waitFor(
      () => {
        expect(mockedDriverService.getDrivers).toHaveBeenLastCalledWith(
          expect.objectContaining({ search: "John" }),
          "test-token"
        );
      },
      { timeout: 1000 }
    );
  });

  it("filters by active status", async () => {
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([makeDriver()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("John Smith");

    await user.selectOptions(screen.getByLabelText("Status:"), "active");

    await waitFor(() => {
      expect(mockedDriverService.getDrivers).toHaveBeenLastCalledWith(
        expect.objectContaining({ isActive: true }),
        "test-token"
      );
    });
  });

  it("filters by availability", async () => {
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([makeDriver()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("John Smith");

    await user.selectOptions(screen.getByLabelText("Availability:"), "available");

    await waitFor(() => {
      expect(mockedDriverService.getDrivers).toHaveBeenLastCalledWith(
        expect.objectContaining({ isAvailable: true }),
        "test-token"
      );
    });
  });

  it("filters by vehicle assignment", async () => {
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([makeDriver()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("John Smith");

    await user.selectOptions(screen.getByLabelText("Vehicle:"), "unassigned");

    await waitFor(() => {
      expect(mockedDriverService.getDrivers).toHaveBeenLastCalledWith(
        expect.objectContaining({ hasVehicle: false }),
        "test-token"
      );
    });
  });

  it("rejects an empty create-driver form client-side", async () => {
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Drivers Found");

    await user.click(screen.getByRole("button", { name: "+ New Driver" }));
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("First name is required.")).toBeInTheDocument();
    expect(mockedDriverService.createDriver).not.toHaveBeenCalled();
  });

  it("creates a driver with an assigned vehicle", async () => {
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([]));
    const vehicle = makeVehicle();
    mockedVehicleService.getVehicles.mockResolvedValue(pagedResult([vehicle]));
    mockedDriverService.createDriver.mockResolvedValue(makeDriver());
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Drivers Found");

    await user.click(screen.getByRole("button", { name: "+ New Driver" }));
    await user.type(screen.getByLabelText("First name"), "John");
    await user.type(screen.getByLabelText("Last name"), "Smith");
    await user.type(screen.getByLabelText("Email"), "john.smith@example.com");
    await user.type(screen.getByLabelText("Phone"), "+41791234567");
    await user.type(screen.getByLabelText("Password"), "Test#Passw0rd!");
    await user.selectOptions(screen.getByLabelText("Vehicle"), vehicle.id);
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(mockedDriverService.createDriver).toHaveBeenCalledWith(
        expect.objectContaining({ email: "john.smith@example.com", vehicleId: vehicle.id }),
        "test-token"
      );
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Driver created successfully.");
  });

  it("shows the backend's duplicate-email error inside the form", async () => {
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([]));
    mockedDriverService.createDriver.mockRejectedValue(new Error("A user with this email already exists."));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Drivers Found");

    await user.click(screen.getByRole("button", { name: "+ New Driver" }));
    await user.type(screen.getByLabelText("First name"), "John");
    await user.type(screen.getByLabelText("Last name"), "Smith");
    await user.type(screen.getByLabelText("Email"), "john.smith@example.com");
    await user.type(screen.getByLabelText("Phone"), "+41791234567");
    await user.type(screen.getByLabelText("Password"), "Test#Passw0rd!");
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("A user with this email already exists.")).toBeInTheDocument();
  });

  it("shows the backend's vehicle-conflict error inside the form", async () => {
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([]));
    mockedDriverService.createDriver.mockRejectedValue(new Error("This vehicle is already assigned to another driver."));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Drivers Found");

    await user.click(screen.getByRole("button", { name: "+ New Driver" }));
    await user.type(screen.getByLabelText("First name"), "John");
    await user.type(screen.getByLabelText("Last name"), "Smith");
    await user.type(screen.getByLabelText("Email"), "john.smith@example.com");
    await user.type(screen.getByLabelText("Phone"), "+41791234567");
    await user.type(screen.getByLabelText("Password"), "Test#Passw0rd!");
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("This vehicle is already assigned to another driver.")).toBeInTheDocument();
  });

  it("edits an existing driver", async () => {
    const driver = makeDriver();
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([driver]));
    mockedDriverService.updateDriver.mockResolvedValue({ ...driver, lastName: "Doe" });
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("John Smith");

    await user.click(screen.getByRole("button", { name: "Edit" }));

    const lastNameInput = screen.getByLabelText("Last name");
    await user.clear(lastNameInput);
    await user.type(lastNameInput, "Doe");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(mockedDriverService.updateDriver).toHaveBeenCalledWith(
        driver.id,
        expect.objectContaining({ lastName: "Doe" }),
        "test-token"
      );
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Driver updated successfully.");
  });

  it("does not deactivate a driver when the confirmation is declined", async () => {
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([makeDriver()]));
    vi.spyOn(window, "confirm").mockReturnValue(false);
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("John Smith");

    await user.click(screen.getByRole("button", { name: "Deactivate" }));

    expect(window.confirm).toHaveBeenCalledWith("Are you sure you want to deactivate this driver?");
    expect(mockedDriverService.deactivateDriver).not.toHaveBeenCalled();
  });

  it("deactivates a driver when the confirmation is accepted", async () => {
    const driver = makeDriver();
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([driver]));
    mockedDriverService.deactivateDriver.mockResolvedValue({ ...driver, isActive: false });
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("John Smith");

    await user.click(screen.getByRole("button", { name: "Deactivate" }));

    await waitFor(() => {
      expect(mockedDriverService.deactivateDriver).toHaveBeenCalledWith(driver.id, "test-token");
    });
  });

  it("resets a driver's password", async () => {
    const driver = makeDriver();
    mockedDriverService.getDrivers.mockResolvedValue(pagedResult([driver]));
    mockedDriverService.resetDriverPassword.mockResolvedValue(driver);
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("John Smith");

    await user.click(screen.getByRole("button", { name: "Reset Password" }));
    const dialog = screen.getByRole("dialog");
    await user.type(within(dialog).getByLabelText("New password"), "NewPassw0rd!23");
    await user.click(within(dialog).getByRole("button", { name: "Reset Password" }));

    await waitFor(() => {
      expect(mockedDriverService.resetDriverPassword).toHaveBeenCalledWith(
        driver.id,
        { newPassword: "NewPassw0rd!23" },
        "test-token"
      );
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Password reset successfully.");
  });
});
