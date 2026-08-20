import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import RoutesPage from "./RoutesPage";
import * as routeService from "../../../services/routeService";
import * as authContext from "../../../context/AuthContext";
import type { RouteDto } from "../../../types/route";
import type { PagedResult } from "../../../types/api";

vi.mock("../../../services/routeService");
vi.mock("../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedRouteService = vi.mocked(routeService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeRoute(overrides: Partial<RouteDto> = {}): RouteDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    departureLocation: "Basel",
    destination: "Zurich",
    estimatedDurationMinutes: 90,
    price: 180,
    currency: "CHF",
    isActive: true,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function pagedResult(items: RouteDto[]): PagedResult<RouteDto> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <RoutesPage />
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  mockedUseAuth.mockReturnValue({
    user: { id: "u1", email: "admin@example.com", firstName: "Admin", lastName: "User", role: "Administrator", languageCode: null },
    accessToken: "test-token",
    expiresAt: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
  });
});

describe("RoutesPage", () => {
  it("renders the route list", async () => {
    mockedRouteService.getRoutes.mockResolvedValue(
      pagedResult([makeRoute(), makeRoute({ id: "2", departureLocation: "Basel", destination: "Bern" })])
    );

    renderPage();

    expect(await screen.findByText("Zurich")).toBeInTheDocument();
    expect(screen.getByText("Bern")).toBeInTheDocument();
  });

  it("shows an error message when loading fails", async () => {
    mockedRouteService.getRoutes.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("searches routes via the backend, not client-side filtering", async () => {
    mockedRouteService.getRoutes.mockResolvedValue(pagedResult([makeRoute()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Zurich");

    await user.type(screen.getByLabelText("Search routes"), "Basel");

    await waitFor(
      () => {
        expect(mockedRouteService.getRoutes).toHaveBeenLastCalledWith(
          expect.objectContaining({ search: "Basel" }),
          "test-token"
        );
      },
      { timeout: 1000 }
    );
  });

  it("filters by active status", async () => {
    mockedRouteService.getRoutes.mockResolvedValue(pagedResult([makeRoute()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Zurich");

    await user.selectOptions(screen.getByLabelText("Status:"), "active");

    await waitFor(() => {
      expect(mockedRouteService.getRoutes).toHaveBeenLastCalledWith(
        expect.objectContaining({ isActive: true }),
        "test-token"
      );
    });
  });

  it("rejects an empty create-route form client-side", async () => {
    mockedRouteService.getRoutes.mockResolvedValue(pagedResult([]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Routes Found");

    await user.click(screen.getByRole("button", { name: "+ New Route" }));
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("Departure location is required.")).toBeInTheDocument();
    expect(mockedRouteService.createRoute).not.toHaveBeenCalled();
  });

  it("edits an existing route", async () => {
    const route = makeRoute();
    mockedRouteService.getRoutes.mockResolvedValue(pagedResult([route]));
    mockedRouteService.updateRoute.mockResolvedValue({ ...route, destination: "Bern" });
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Zurich");

    await user.click(screen.getByRole("button", { name: "Edit" }));

    const destinationInput = screen.getByLabelText("Destination");
    await user.clear(destinationInput);
    await user.type(destinationInput, "Bern");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(mockedRouteService.updateRoute).toHaveBeenCalledWith(
        route.id,
        expect.objectContaining({ destination: "Bern" }),
        "test-token"
      );
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Route updated successfully.");
  });

  it("does not deactivate a route when the confirmation is declined", async () => {
    mockedRouteService.getRoutes.mockResolvedValue(pagedResult([makeRoute()]));
    vi.spyOn(window, "confirm").mockReturnValue(false);
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Zurich");

    await user.click(screen.getByRole("button", { name: "Deactivate" }));

    expect(window.confirm).toHaveBeenCalledWith("Are you sure you want to deactivate this route?");
    expect(mockedRouteService.deactivateRoute).not.toHaveBeenCalled();
  });

  it("deactivates a route when the confirmation is accepted", async () => {
    const route = makeRoute();
    mockedRouteService.getRoutes.mockResolvedValue(pagedResult([route]));
    mockedRouteService.deactivateRoute.mockResolvedValue({ ...route, isActive: false });
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Zurich");

    await user.click(screen.getByRole("button", { name: "Deactivate" }));

    await waitFor(() => {
      expect(mockedRouteService.deactivateRoute).toHaveBeenCalledWith(route.id, "test-token");
    });
  });
});
