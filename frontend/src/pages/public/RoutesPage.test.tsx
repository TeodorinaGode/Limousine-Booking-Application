import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import RoutesPage from "./RoutesPage";
import { AuthProvider } from "../../context/AuthContext";
import * as bookingService from "../../services/bookingService";
import * as companyService from "../../services/companyService";
import type { PublicRouteDto } from "../../types/booking";

vi.mock("../../services/bookingService");
vi.mock("../../services/companyService");

const mockedBookingService = vi.mocked(bookingService);
const mockedCompanyService = vi.mocked(companyService);

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
  });
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
});
