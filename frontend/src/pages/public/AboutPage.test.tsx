import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import AboutPage from "./AboutPage";
import { AuthProvider } from "../../context/AuthContext";
import * as companyService from "../../services/companyService";

vi.mock("../../services/companyService");

const mockedCompanyService = vi.mocked(companyService);

function renderPage() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <AboutPage />
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

describe("AboutPage", () => {
  it("renders the about content and why-choose-us items", () => {
    renderPage();

    expect(screen.getByRole("heading", { name: "About Our Service", level: 1 })).toBeInTheDocument();
    expect(screen.getByText("Reliability")).toBeInTheDocument();
    expect(screen.getByText("Luxury")).toBeInTheDocument();
    expect(screen.getByText("Professional Chauffeurs")).toBeInTheDocument();
  });

  it("links the final call to action to the booking flow", () => {
    renderPage();

    const bookingLinks = screen.getAllByRole("link", { name: "Book a Ride" });
    expect(bookingLinks.length).toBeGreaterThan(0);
    for (const link of bookingLinks) {
      expect(link).toHaveAttribute("href", "/booking");
    }
  });
});
