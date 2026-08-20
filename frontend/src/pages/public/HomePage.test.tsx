import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import HomePage from "./HomePage";
import { AuthProvider } from "../../context/AuthContext";
import * as bookingService from "../../services/bookingService";
import * as publicVehicleService from "../../services/publicVehicleService";
import * as companyService from "../../services/companyService";
import * as locationService from "../../services/locationService";
import type { PublicRouteDto } from "../../types/booking";
import type { PublicVehicleDto } from "../../types/publicVehicle";
import type { PublicLocationsDto } from "../../types/location";

vi.mock("../../services/bookingService");
vi.mock("../../services/publicVehicleService");
vi.mock("../../services/companyService");
vi.mock("../../services/locationService");
vi.mock("../../components/ServiceAreaMap", () => ({
  default: () => <div data-testid="service-area-map" />,
}));

const mockedBookingService = vi.mocked(bookingService);
const mockedPublicVehicleService = vi.mocked(publicVehicleService);
const mockedCompanyService = vi.mocked(companyService);
const mockedLocationService = vi.mocked(locationService);

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
const enabledLocations: PublicLocationsDto = {
  enabled: true,
  provider: "leaflet",
  defaultLatitude: 47.0,
  defaultLongitude: 8.5,
  defaultZoom: 6,
  locations: [
    { id: "loc-1", name: "Basel", countryCode: "CH", latitude: 47.5596, longitude: 7.5886, type: "City", description: null },
  ],
};

beforeEach(() => {
  vi.clearAllMocks();
  mockedBookingService.getActiveRoutes.mockResolvedValue(routes);
  mockedPublicVehicleService.getActiveVehicles.mockResolvedValue(vehicles);
  mockedLocationService.getLocations.mockResolvedValue(enabledLocations);
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

describe("HomePage", () => {
  it("renders the hero with the primary and secondary calls to action", () => {
    renderPage();

    expect(screen.getByRole("heading", { level: 1 }).textContent).toContain("Luxury Travel.");
    expect(screen.getAllByRole("link", { name: "Book a Ride" }).length).toBeGreaterThan(0);
    expect(screen.getByRole("button", { name: "Explore Our Services" })).toBeInTheDocument();
  });

  it("shows the trust indicators", () => {
    renderPage();

    expect(screen.getByText("Private Chauffeur")).toBeInTheDocument();
    expect(screen.getByText("On-Time Service")).toBeInTheDocument();
    expect(screen.getAllByText("Premium Vehicles").length).toBeGreaterThan(0);
  });

  it("lists all five real ROI Limousinen services with a booking CTA each", () => {
    renderPage();

    expect(screen.getAllByText("Airport Transfer").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Professional Chauffeur Service").length).toBeGreaterThan(0);
    expect(screen.getAllByRole("link", { name: "Book Your Transfer" })).toHaveLength(5);
  });

  it("shows the real operating countries, sourced from the backend", async () => {
    renderPage();

    expect(await screen.findByText("Switzerland")).toBeInTheDocument();
    expect(screen.getByText("Austria")).toBeInTheDocument();
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

  it("shows the service area map section when locations are enabled and available", async () => {
    renderPage();

    expect(await screen.findByTestId("service-area-map")).toBeInTheDocument();
    expect(screen.getByText("Chauffeur Services Across Switzerland and Europe")).toBeInTheDocument();
  });

  it("does not render the map section when the backend disables it", async () => {
    mockedLocationService.getLocations.mockResolvedValue({ ...enabledLocations, enabled: false, locations: [] });
    renderPage();

    await screen.findByText("Choose Your Route");
    expect(screen.queryByTestId("service-area-map")).not.toBeInTheDocument();
  });

  it("does not render the map section when there are no locations", async () => {
    mockedLocationService.getLocations.mockResolvedValue({ ...enabledLocations, locations: [] });
    renderPage();

    await screen.findByText("Choose Your Route");
    expect(screen.queryByTestId("service-area-map")).not.toBeInTheDocument();
  });
});
