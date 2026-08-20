import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import LegalPage from "./LegalPage";
import { AuthProvider } from "../../context/AuthContext";
import * as companyService from "../../services/companyService";

vi.mock("../../services/companyService");

const mockedCompanyService = vi.mocked(companyService);

function renderPage(titleKey: "privacyTitle" | "termsTitle" | "cookieTitle") {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <LegalPage titleKey={titleKey} />
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
  });
});

describe("LegalPage", () => {
  it("renders the privacy policy title and an honest placeholder notice", () => {
    renderPage("privacyTitle");

    expect(screen.getByRole("heading", { name: "Privacy Policy", level: 1 })).toBeInTheDocument();
    expect(screen.getByText(/actual legal text has not yet been provided/)).toBeInTheDocument();
  });

  it("renders the terms and conditions title", () => {
    renderPage("termsTitle");

    expect(screen.getByRole("heading", { name: "Terms & Conditions", level: 1 })).toBeInTheDocument();
  });

  it("renders the cookie policy title", () => {
    renderPage("cookieTitle");

    expect(screen.getByRole("heading", { name: "Cookie Policy", level: 1 })).toBeInTheDocument();
  });
});
