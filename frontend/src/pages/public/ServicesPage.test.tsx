import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ServicesPage from "./ServicesPage";
import { AuthProvider } from "../../context/AuthContext";
import * as companyService from "../../services/companyService";

vi.mock("../../services/companyService");

const mockedCompanyService = vi.mocked(companyService);

function renderPage() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <ServicesPage />
      </AuthProvider>
    </MemoryRouter>
  );
}

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

describe("ServicesPage", () => {
  it("lists all five real Limousinen services as an editorial list, each linking to booking", () => {
    renderPage();

    expect(screen.getAllByText("Airport Transfer").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Corporate Events").length).toBeGreaterThan(0);
    expect(screen.getAllByText("City Tours").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Point-to-Point Transfers").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Professional Chauffeur Service").length).toBeGreaterThan(0);
    expect(screen.getByRole("link", { name: /01.*Airport Transfer/s })).toHaveAttribute("href", "/booking");
  });

  it("highlights airport transfers with a dedicated call to action", () => {
    renderPage();

    expect(screen.getByRole("link", { name: "Book Airport Transfer" })).toBeInTheDocument();
  });
});
