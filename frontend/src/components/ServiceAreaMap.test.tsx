import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import ServiceAreaMap from "./ServiceAreaMap";
import type { PublicLocationDto } from "../types/location";
import type { PublicRouteDto } from "../types/booking";

/**
 * react-leaflet renders real DOM measurements Leaflet needs that jsdom
 * doesn't provide, so — as is standard practice for testing react-leaflet
 * consumers — the library is mocked here with simple stubs that expose the
 * same props ServiceAreaMap passes them. This tests ServiceAreaMap's own
 * logic (route/location matching, click handling, navigation) rather than
 * Leaflet's rendering, which is out of scope for a unit test anyway.
 */
vi.mock("react-leaflet", () => ({
  MapContainer: ({ children }: { children: React.ReactNode }) => <div data-testid="map-container">{children}</div>,
  TileLayer: () => null,
  Circle: () => null,
  Marker: ({ children }: { children: React.ReactNode }) => <div data-testid="marker">{children}</div>,
  Popup: ({ children }: { children: React.ReactNode }) => <div data-testid="popup">{children}</div>,
  Polyline: ({ eventHandlers }: { eventHandlers?: { click?: () => void } }) => (
    <button type="button" data-testid="polyline" onClick={eventHandlers?.click}>
      route line
    </button>
  ),
  useMap: () => ({ flyToBounds: vi.fn() }),
}));

let capturedLocationState: unknown = null;

function RouteStateCapture() {
  const location = useLocation();
  capturedLocationState = location.state;
  return <p data-testid="probe">probed</p>;
}

function renderMap(props: Partial<React.ComponentProps<typeof ServiceAreaMap>> = {}) {
  capturedLocationState = null;
  const defaultProps: React.ComponentProps<typeof ServiceAreaMap> = {
    locations,
    routes,
    defaultLatitude: 47.0,
    defaultLongitude: 8.5,
    defaultZoom: 6,
  };
  return render(
    <MemoryRouter initialEntries={["/routes"]}>
      <Routes>
        <Route path="/routes" element={<ServiceAreaMap {...defaultProps} {...props} />} />
        <Route path="/booking" element={<RouteStateCapture />} />
      </Routes>
    </MemoryRouter>
  );
}

const locations: PublicLocationDto[] = [
  { id: "loc-basel", name: "Basel", countryCode: "CH", latitude: 47.5596, longitude: 7.5886, type: "City", description: "Major Swiss city" },
  { id: "loc-zurich", name: "Zurich", countryCode: "CH", latitude: 47.3769, longitude: 8.5417, type: "City", description: "Major Swiss city" },
  { id: "loc-milan", name: "Milan", countryCode: "IT", latitude: 45.4642, longitude: 9.19, type: "Destination", description: "Nearby European destination" },
];

const routes: PublicRouteDto[] = [
  { id: "route-1", departureLocation: "Basel", destination: "Zurich", estimatedDurationMinutes: 60, price: 180, currency: "CHF" },
  { id: "route-2", departureLocation: "Basel", destination: "Milan", estimatedDurationMinutes: 240, price: 600, currency: "CHF" },
];

describe("ServiceAreaMap", () => {
  it("renders a marker for every location, including ones with no bookable route", () => {
    renderMap();

    expect(screen.getAllByTestId("marker")).toHaveLength(3);
    expect(screen.getAllByText("Milan").length).toBeGreaterThan(0);
  });

  it("only draws a route line when both endpoints match a real location", () => {
    renderMap();

    // route-1 (Basel -> Zurich) matches two real locations; route-2 (Basel -> Milan)
    // also matches, since Milan is seeded as a location too.
    expect(screen.getAllByTestId("polyline")).toHaveLength(2);
  });

  it("does not draw a line for a route whose endpoint has no matching location", () => {
    const routesWithUnmatched: PublicRouteDto[] = [
      ...routes,
      { id: "route-3", departureLocation: "Basel", destination: "Timbuktu", estimatedDurationMinutes: 999, price: 999, currency: "CHF" },
    ];
    renderMap({ routes: routesWithUnmatched });

    expect(screen.getAllByTestId("polyline")).toHaveLength(2);
  });

  it("shows available destinations with price for a departure location, in a marker popup", () => {
    renderMap();

    expect(screen.getByText(/Zurich · CHF 180/)).toBeInTheDocument();
    expect(screen.getByText(/Milan · CHF 600/)).toBeInTheDocument();
  });

  it("shows a generic booking link, not a route list, for a location with no departing routes", () => {
    renderMap({ routes: [] });

    const bookingLinks = screen.getAllByRole("link", { name: "Book a Ride" });
    expect(bookingLinks.length).toBe(locations.length);
  });

  it("navigates to booking with the matched route id when a popup destination button is clicked", async () => {
    const user = userEvent.setup();
    renderMap();

    await user.click(screen.getByText(/Zurich · CHF 180/));

    expect(await screen.findByTestId("probe")).toBeInTheDocument();
    expect(capturedLocationState).toEqual({ routeId: "route-1" });
  });

  it("selects a route and shows its info card when a route line is clicked", async () => {
    const user = userEvent.setup();
    const onSelectRoute = vi.fn();
    renderMap({ onSelectRoute });

    await user.click(screen.getAllByTestId("polyline")[0]);

    expect(onSelectRoute).toHaveBeenCalledWith("route-1");
    expect(screen.getByText("Book This Route")).toBeInTheDocument();
  });

  it("navigates to booking with the route id when Book This Route is clicked", async () => {
    const user = userEvent.setup();
    renderMap();

    await user.click(screen.getAllByTestId("polyline")[0]);
    await user.click(screen.getByText("Book This Route"));

    expect(await screen.findByTestId("probe")).toBeInTheDocument();
    expect(capturedLocationState).toEqual({ routeId: "route-1" });
  });

  it("always renders an accessible text list of destinations, independent of the visual map", () => {
    renderMap();

    expect(screen.getByText("Main Destinations")).toBeInTheDocument();
    const list = screen.getByText("Main Destinations").closest(".map-location-list");
    expect(list).not.toBeNull();
    expect(list!.querySelectorAll("li")).toHaveLength(3);
  });
});
