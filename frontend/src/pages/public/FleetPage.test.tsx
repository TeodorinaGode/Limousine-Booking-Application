import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import FleetPage from "./FleetPage";
import { AuthProvider } from "../../context/AuthContext";
import * as publicVehicleService from "../../services/publicVehicleService";
import * as companyService from "../../services/companyService";
import type { PublicVehicleDto } from "../../types/publicVehicle";

vi.mock("../../services/publicVehicleService");
vi.mock("../../services/companyService");

const mockedPublicVehicleService = vi.mocked(publicVehicleService);
const mockedCompanyService = vi.mocked(companyService);

function renderPage() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <FleetPage />
      </AuthProvider>
    </MemoryRouter>
  );
}

const vehicles: PublicVehicleDto[] = [
  { id: "vehicle-1", make: "Mercedes-Benz", model: "S-Class", vehicleType: "Sedan", passengerCapacity: 3 },
  { id: "vehicle-2", make: "Mercedes-Benz", model: "V-Class", vehicleType: "Van", passengerCapacity: 6 },
];

beforeEach(() => {
  vi.clearAllMocks();
  mockedCompanyService.getCompanyInfo.mockResolvedValue({
    companyName: "Test Chauffeur",
    tagline: "Test Tagline",
    phone: "+41 79 000 00 00",
    email: "info@example.com",
    address: "Bahnhofplatz 1, Basel",
    website: "",
    openingHours: "",
    emergencyPhone: null,
    description: null,
    operatingCountryCodes: ["CH", "AT"],
    facebookUrl: null,
    instagramUrl: null,
    whatsAppUrl: null,
  });
});

describe("FleetPage", () => {
  it("loads and displays active vehicles from the backend, never registration numbers or notes", async () => {
    mockedPublicVehicleService.getActiveVehicles.mockResolvedValue(vehicles);
    renderPage();

    expect(await screen.findByText("Mercedes-Benz S-Class")).toBeInTheDocument();
    expect(screen.getByText("Mercedes-Benz V-Class")).toBeInTheDocument();
    expect(mockedPublicVehicleService.getActiveVehicles).toHaveBeenCalled();
  });

  it("shows the passenger capacity for each vehicle", async () => {
    mockedPublicVehicleService.getActiveVehicles.mockResolvedValue(vehicles);
    renderPage();

    expect(await screen.findByText(/Passengers: 3/)).toBeInTheDocument();
    expect(screen.getByText(/Passengers: 6/)).toBeInTheDocument();
  });

  it("shows an empty state when there are no active vehicles", async () => {
    mockedPublicVehicleService.getActiveVehicles.mockResolvedValue([]);
    renderPage();

    expect(await screen.findByText("No vehicles are currently available.")).toBeInTheDocument();
  });

  it("shows an error message when the fleet request fails", async () => {
    mockedPublicVehicleService.getActiveVehicles.mockRejectedValue(new Error("Service unavailable"));
    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Service unavailable");
  });
});
