import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import SettingsPage from "./SettingsPage";
import * as companyService from "../../services/companyService";
import * as authContext from "../../context/AuthContext";
import type { CompanyInfoDto } from "../../types/company";

vi.mock("../../services/companyService");
vi.mock("../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedCompanyService = vi.mocked(companyService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeCompany(overrides: Partial<CompanyInfoDto> = {}): CompanyInfoDto {
  return {
    companyName: "ROI Limousinen",
    tagline: "Premium Chauffeur Service",
    phone: "+41 79 000 00 00",
    email: "info@example.com",
    address: "Switzerland",
    website: "https://roi-limousinen.example",
    openingHours: "24/7",
    emergencyPhone: null,
    description: null,
    operatingCountryCodes: ["CH", "AT"],
    facebookUrl: null,
    instagramUrl: null,
    whatsAppUrl: null,
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <SettingsPage />
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

describe("SettingsPage", () => {
  it("shows the company's general, contact, and operating-area information", async () => {
    mockedCompanyService.getCompanyInfo.mockResolvedValue(makeCompany());

    renderPage();

    expect(await screen.findByText("ROI Limousinen")).toBeInTheDocument();
    expect(screen.getByText("+41 79 000 00 00")).toBeInTheDocument();
    expect(screen.getByText("info@example.com")).toBeInTheDocument();
    expect(screen.getByText("CH")).toBeInTheDocument();
    expect(screen.getByText("AT")).toBeInTheDocument();
  });

  it("shows an explicit placeholder for fields with no value, never a blank row", async () => {
    mockedCompanyService.getCompanyInfo.mockResolvedValue(makeCompany({ emergencyPhone: null, description: null }));

    renderPage();

    await screen.findByText("ROI Limousinen");
    const emergencyPhoneRow = screen.getByText("Emergency Phone");
    expect(emergencyPhoneRow.nextElementSibling).toHaveTextContent("—");
  });

  it("shows a message instead of an empty section when no social links are configured", async () => {
    mockedCompanyService.getCompanyInfo.mockResolvedValue(makeCompany());

    renderPage();

    expect(await screen.findByText("No social media links configured.")).toBeInTheDocument();
  });

  it("shows an error when settings fail to load", async () => {
    mockedCompanyService.getCompanyInfo.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });
});
