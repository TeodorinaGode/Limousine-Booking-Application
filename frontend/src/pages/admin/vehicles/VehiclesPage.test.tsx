import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import VehiclesPage from "./VehiclesPage";
import * as vehicleService from "../../../services/vehicleService";
import * as authContext from "../../../context/AuthContext";
import type { VehicleDto } from "../../../types/vehicle";
import type { PagedResult } from "../../../types/api";

vi.mock("../../../services/vehicleService");
vi.mock("../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedVehicleService = vi.mocked(vehicleService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeVehicle(overrides: Partial<VehicleDto> = {}): VehicleDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
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

function pagedResult(items: VehicleDto[]): PagedResult<VehicleDto> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <VehiclesPage />
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

describe("VehiclesPage", () => {
  it("renders the vehicle list", async () => {
    mockedVehicleService.getVehicles.mockResolvedValue(
      pagedResult([makeVehicle(), makeVehicle({ id: "2", registrationNumber: "BS 789012", model: "S-Class" })])
    );

    renderPage();

    expect(await screen.findByText("V-Class")).toBeInTheDocument();
    expect(screen.getByText("S-Class")).toBeInTheDocument();
  });

  it("shows an error message when loading fails", async () => {
    mockedVehicleService.getVehicles.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("searches vehicles via the backend, not client-side filtering", async () => {
    mockedVehicleService.getVehicles.mockResolvedValue(pagedResult([makeVehicle()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("V-Class");

    await user.type(screen.getByLabelText("Search vehicles"), "Mercedes");

    await waitFor(
      () => {
        expect(mockedVehicleService.getVehicles).toHaveBeenLastCalledWith(
          expect.objectContaining({ search: "Mercedes" }),
          "test-token"
        );
      },
      { timeout: 1000 }
    );
  });

  it("filters by active status", async () => {
    mockedVehicleService.getVehicles.mockResolvedValue(pagedResult([makeVehicle()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("V-Class");

    await user.selectOptions(screen.getByLabelText("Status:"), "active");

    await waitFor(() => {
      expect(mockedVehicleService.getVehicles).toHaveBeenLastCalledWith(
        expect.objectContaining({ isActive: true }),
        "test-token"
      );
    });
  });

  it("filters by minimum passenger capacity", async () => {
    mockedVehicleService.getVehicles.mockResolvedValue(pagedResult([makeVehicle()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("V-Class");

    await user.selectOptions(screen.getByLabelText("Capacity:"), "5+");

    await waitFor(() => {
      expect(mockedVehicleService.getVehicles).toHaveBeenLastCalledWith(
        expect.objectContaining({ minCapacity: 5 }),
        "test-token"
      );
    });
  });

  it("rejects an empty create-vehicle form client-side", async () => {
    mockedVehicleService.getVehicles.mockResolvedValue(pagedResult([]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Vehicles Found");

    await user.click(screen.getByRole("button", { name: "+ New Vehicle" }));
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("Registration number is required.")).toBeInTheDocument();
    expect(mockedVehicleService.createVehicle).not.toHaveBeenCalled();
  });

  it("shows the backend's duplicate-registration error inside the form", async () => {
    mockedVehicleService.getVehicles.mockResolvedValue(pagedResult([]));
    mockedVehicleService.createVehicle.mockRejectedValue(
      new Error("A vehicle with this registration number already exists.")
    );
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Vehicles Found");

    await user.click(screen.getByRole("button", { name: "+ New Vehicle" }));
    await user.type(screen.getByLabelText("Registration number"), "BS 123456");
    await user.type(screen.getByLabelText("Make"), "Mercedes-Benz");
    await user.type(screen.getByLabelText("Model"), "V-Class");
    await user.type(screen.getByLabelText("Vehicle type"), "Van");
    const capacityInput = screen.getByLabelText("Passenger capacity");
    await user.clear(capacityInput);
    await user.type(capacityInput, "7");
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("A vehicle with this registration number already exists.")).toBeInTheDocument();
  });

  it("edits an existing vehicle", async () => {
    const vehicle = makeVehicle();
    mockedVehicleService.getVehicles.mockResolvedValue(pagedResult([vehicle]));
    mockedVehicleService.updateVehicle.mockResolvedValue({ ...vehicle, model: "S-Class" });
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("V-Class");

    await user.click(screen.getByRole("button", { name: "Edit" }));

    const modelInput = screen.getByLabelText("Model");
    await user.clear(modelInput);
    await user.type(modelInput, "S-Class");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(mockedVehicleService.updateVehicle).toHaveBeenCalledWith(
        vehicle.id,
        expect.objectContaining({ model: "S-Class" }),
        "test-token"
      );
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Vehicle updated successfully.");
  });

  it("does not deactivate a vehicle when the confirmation is declined", async () => {
    mockedVehicleService.getVehicles.mockResolvedValue(pagedResult([makeVehicle()]));
    vi.spyOn(window, "confirm").mockReturnValue(false);
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("V-Class");

    await user.click(screen.getByRole("button", { name: "Deactivate" }));

    expect(window.confirm).toHaveBeenCalledWith("Are you sure you want to deactivate this vehicle?");
    expect(mockedVehicleService.deactivateVehicle).not.toHaveBeenCalled();
  });

  it("deactivates a vehicle when the confirmation is accepted", async () => {
    const vehicle = makeVehicle();
    mockedVehicleService.getVehicles.mockResolvedValue(pagedResult([vehicle]));
    mockedVehicleService.deactivateVehicle.mockResolvedValue({ ...vehicle, isActive: false });
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("V-Class");

    await user.click(screen.getByRole("button", { name: "Deactivate" }));

    await waitFor(() => {
      expect(mockedVehicleService.deactivateVehicle).toHaveBeenCalledWith(vehicle.id, "test-token");
    });
  });
});
