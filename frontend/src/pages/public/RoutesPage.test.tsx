import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import RoutesPage from "./RoutesPage";
import { AuthProvider } from "../../context/AuthContext";
import * as bookingService from "../../services/bookingService";
import * as companyService from "../../services/companyService";
import * as locationService from "../../services/locationService";
import type { PublicRouteDto } from "../../types/booking";
import type { PublicLocationsDto } from "../../types/location";

vi.mock("../../services/bookingService");
vi.mock("../../services/companyService");
vi.mock("../../services/locationService");
vi.mock("../../components/ServiceAreaMap", () => ({
  default: ({ selectedRouteId, onSelectRoute }: { selectedRouteId?: string | null; onSelectRoute?: (id: string) => void }) => (
    <div data-testid="service-area-map">
      <p>selected: {selectedRouteId ?? "none"}</p>
      <button type="button" onClick={() => onSelectRoute?.("route-2")}>
        select route-2 on map
      </button>
    </div>
  ),
}));

const mockedBookingService = vi.mocked(bookingService);
const mockedCompanyService = vi.mocked(companyService);
const mockedLocationService = vi.mocked(locationService);

function renderPage() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <RoutesPage />
      </AuthProvider>
    </MemoryRouter>
  );
}

const routes: PublicRouteDto[] = [
  { id: "route-1", departureLocation: "Basel", destination: "Zurich", estimatedDurationMinutes: 90, price: 180, currency: "CHF" },
  { id: "route-2", departureLocation: "Geneva", destination: "Bern", estimatedDurationMinutes: 75, price: 210, currency: "CHF" },
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
  mockedLocationService.getLocations.mockResolvedValue({
    enabled: false,
    provider: "leaflet",
    defaultLatitude: 47.0,
    defaultLongitude: 8.5,
    defaultZoom: 6,
    locations: [],
  } satisfies PublicLocationsDto);
});

describe("RoutesPage", () => {
  it("loads and displays all active routes from the backend", async () => {
    mockedBookingService.getActiveRoutes.mockResolvedValue(routes);
    renderPage();

    expect(await screen.findByText("Basel")).toBeInTheDocument();
    expect(screen.getByText("Zurich")).toBeInTheDocument();
    expect(screen.getByText("Geneva")).toBeInTheDocument();
    expect(mockedBookingService.getActiveRoutes).toHaveBeenCalled();
  });

  it("links each route to the booking flow with the route id preselected", async () => {
    mockedBookingService.getActiveRoutes.mockResolvedValue(routes);
    renderPage();

    await screen.findByText("Basel");
    const bookLinks = screen.getAllByRole("link", { name: "Book This Route" });
    expect(bookLinks).toHaveLength(2);
    expect(bookLinks[0]).toHaveAttribute("href", "/booking");
  });

  it("shows an empty state when there are no active routes", async () => {
    mockedBookingService.getActiveRoutes.mockResolvedValue([]);
    renderPage();

    expect(await screen.findByText("No routes are currently available.")).toBeInTheDocument();
  });

  it("shows an error message when the routes request fails", async () => {
    mockedBookingService.getActiveRoutes.mockRejectedValue(new Error("Network error"));
    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("shows the map alongside the route list when locations are enabled and available", async () => {
    mockedBookingService.getActiveRoutes.mockResolvedValue(routes);
    mockedLocationService.getLocations.mockResolvedValue({
      enabled: true,
      provider: "leaflet",
      defaultLatitude: 47.0,
      defaultLongitude: 8.5,
      defaultZoom: 6,
      locations: [{ id: "loc-1", name: "Basel", countryCode: "CH", latitude: 47.5596, longitude: 7.5886, type: "City", description: null }],
    } satisfies PublicLocationsDto);
    renderPage();

    expect(await screen.findByTestId("service-area-map")).toBeInTheDocument();
    expect(screen.getAllByRole("link", { name: "Book This Route" })).toHaveLength(2);
  });

  it("selecting a route on the map highlights the matching card in the list", async () => {
    const user = userEvent.setup();
    mockedBookingService.getActiveRoutes.mockResolvedValue(routes);
    mockedLocationService.getLocations.mockResolvedValue({
      enabled: true,
      provider: "leaflet",
      defaultLatitude: 47.0,
      defaultLongitude: 8.5,
      defaultZoom: 6,
      locations: [{ id: "loc-1", name: "Basel", countryCode: "CH", latitude: 47.5596, longitude: 7.5886, type: "City", description: null }],
    } satisfies PublicLocationsDto);
    renderPage();

    await screen.findByTestId("service-area-map");
    await user.click(screen.getByRole("button", { name: "select route-2 on map" }));

    expect(screen.getByText("selected: route-2")).toBeInTheDocument();
    const geneva = screen.getByText("Geneva").closest(".route-list-item");
    expect(geneva).toHaveClass("route-list-item--active");
  });

  it("selecting a route card in the list passes its id down to the map", async () => {
    const user = userEvent.setup();
    mockedBookingService.getActiveRoutes.mockResolvedValue(routes);
    mockedLocationService.getLocations.mockResolvedValue({
      enabled: true,
      provider: "leaflet",
      defaultLatitude: 47.0,
      defaultLongitude: 8.5,
      defaultZoom: 6,
      locations: [{ id: "loc-1", name: "Basel", countryCode: "CH", latitude: 47.5596, longitude: 7.5886, type: "City", description: null }],
    } satisfies PublicLocationsDto);
    renderPage();

    await screen.findByTestId("service-area-map");
    expect(screen.getByText("selected: none")).toBeInTheDocument();

    const baselCard = screen.getByText("Basel").closest(".route-list-item")!;
    await user.click(baselCard);

    expect(screen.getByText("selected: route-1")).toBeInTheDocument();
  });
});
