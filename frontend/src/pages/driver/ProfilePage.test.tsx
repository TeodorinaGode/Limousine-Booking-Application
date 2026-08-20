import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ProfilePage from "./ProfilePage";
import * as driverBookingService from "../../services/driverBookingService";
import * as authContext from "../../context/AuthContext";
import type { DriverDto } from "../../types/driver";

vi.mock("../../services/driverBookingService");
vi.mock("../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedDriverBookingService = vi.mocked(driverBookingService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeProfile(overrides: Partial<DriverDto> = {}): DriverDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    firstName: "John",
    lastName: "Driver",
    email: "john.driver@example.com",
    phone: "+41791234567",
    isActive: true,
    isAvailable: true,
    vehicle: { id: "22222222-2222-2222-2222-222222222222", registrationNumber: "BS 999999", make: "Mercedes-Benz", model: "E-Class" },
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <ProfilePage />
    </MemoryRouter>
  );
}

beforeEach(() => {
  vi.clearAllMocks();
  mockedUseAuth.mockReturnValue({
    user: { id: "u1", email: "john.driver@example.com", firstName: "John", lastName: "Driver", role: "Driver", languageCode: null },
    accessToken: "test-token",
    expiresAt: null,
    isAuthenticated: true,
    login: vi.fn(),
    logout: vi.fn(),
  });
});

describe("ProfilePage", () => {
  it("shows the driver's contact info, status, and vehicle", async () => {
    mockedDriverBookingService.getMyProfile.mockResolvedValue(makeProfile());

    renderPage();

    expect(await screen.findByText("john.driver@example.com", { exact: false })).toBeInTheDocument();
    expect(screen.getByText("Account: Active")).toBeInTheDocument();
    expect(screen.getByText("Availability: Available")).toBeInTheDocument();
    expect(screen.getByText(/Mercedes-Benz E-Class/)).toBeInTheDocument();
  });

  it("shows a message when no vehicle is assigned", async () => {
    mockedDriverBookingService.getMyProfile.mockResolvedValue(makeProfile({ vehicle: null }));

    renderPage();

    expect(await screen.findByText("No vehicle assigned.")).toBeInTheDocument();
  });

  it("shows an error when the profile fails to load", async () => {
    mockedDriverBookingService.getMyProfile.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });
});
