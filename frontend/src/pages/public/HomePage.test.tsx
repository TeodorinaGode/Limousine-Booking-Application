import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import HomePage from "./HomePage";
import { AuthProvider } from "../../context/AuthContext";
import * as bookingService from "../../services/bookingService";
import * as publicVehicleService from "../../services/publicVehicleService";
import * as companyService from "../../services/companyService";
import type { PublicRouteDto } from "../../types/booking";
import type { PublicVehicleDto } from "../../types/publicVehicle";

vi.mock("../../services/bookingService");
vi.mock("../../services/publicVehicleService");
vi.mock("../../services/companyService");

const mockedBookingService = vi.mocked(bookingService);
const mockedPublicVehicleService = vi.mocked(publicVehicleService);
const mockedCompanyService = vi.mocked(companyService);

function renderPage() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <HomePage />
      </AuthProvider>
    </MemoryRouter>
  );
}

const routes: PublicRouteDto[] = [
  { id: "route-1", departureLocation: "Basel", destination: "Zurich", estimatedDurationMinutes: 60, price: 180, currency: "CHF" },
];
const vehicles: PublicVehicleDto[] = [
  { id: "vehicle-1", make: "Mercedes-Benz", model: "S-Class", vehicleType: "Sedan", passengerCapacity: 3 },
];

beforeEach(() => {
  vi.clearAllMocks();
  mockedBookingService.getActiveRoutes.mockResolvedValue(routes);
  mockedPublicVehicleService.getActiveVehicles.mockResolvedValue(vehicles);
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
  });
});

describe("HomePage", () => {
  it("renders the hero with the primary and secondary calls to action", () => {
    renderPage();

    expect(screen.getByRole("heading", { level: 1 }).textContent).toContain("Travel in comfort.");
    expect(screen.getAllByRole("link", { name: "Book a Ride" }).length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: "Explore Our Services" })).toBeInTheDocument();
  });

  it("shows the trust indicators", () => {
    renderPage();

    expect(screen.getByText("Private Chauffeur")).toBeInTheDocument();
    expect(screen.getByText("On-Time Service")).toBeInTheDocument();
    expect(screen.getAllByText("Premium Vehicles").length).toBeGreaterThan(0);
  });

  it("lists all six marketing services with a booking CTA each", () => {
    renderPage();

    expect(screen.getAllByText("Airport Transfers").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Private Chauffeur Service").length).toBeGreaterThan(0);
    expect(screen.getAllByRole("link", { name: "Book Your Transfer" })).toHaveLength(6);
  });

  it("loads and displays popular routes from the backend, never hard-coded", async () => {
    renderPage();

    await screen.findAllByText("Basel");
    expect(screen.getAllByText("Basel").length).toBeGreaterThan(0);
    expect(screen.getByText("Zurich")).toBeInTheDocument();
    expect(mockedBookingService.getActiveRoutes).toHaveBeenCalled();
  });

  it("loads and displays fleet vehicles from the backend", async () => {
    renderPage();

    expect(await screen.findByText("Mercedes-Benz S-Class")).toBeInTheDocument();
    expect(mockedPublicVehicleService.getActiveVehicles).toHaveBeenCalled();
  });

  it("includes the how-it-works steps", () => {
    renderPage();

    expect(screen.getByText("Choose Your Route")).toBeInTheDocument();
    expect(screen.getByText("Confirm & Pay")).toBeInTheDocument();
  });
});
