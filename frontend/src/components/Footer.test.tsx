import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Footer from "./Footer";
import * as companyService from "../services/companyService";

vi.mock("../services/companyService");

const mockedCompanyService = vi.mocked(companyService);

function renderFooter() {
  return render(
    <MemoryRouter>
      <Footer />
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

describe("Footer", () => {
  it("renders navigation and legal links", () => {
    renderFooter();

    expect(screen.getByRole("link", { name: "Privacy Policy" })).toHaveAttribute("href", "/privacy-policy");
    expect(screen.getByRole("link", { name: "Terms & Conditions" })).toHaveAttribute("href", "/terms-and-conditions");
    expect(screen.getByRole("link", { name: "Cookie Policy" })).toHaveAttribute("href", "/cookie-policy");
  });

  it("shows the current year and company name in the bottom bar", () => {
    renderFooter();

    const year = new Date().getFullYear().toString();
    expect(screen.getByText(new RegExp(year))).toBeInTheDocument();
  });

  it("displays fetched company contact details", async () => {
    renderFooter();

    expect(await screen.findByText("+41 79 000 00 00")).toBeInTheDocument();
    expect(screen.getByText("info@example.com")).toBeInTheDocument();
    expect(screen.getByText("Bahnhofplatz 1, Basel")).toBeInTheDocument();
  });

  it("only lists services that are actually configured, never invented ones", () => {
    renderFooter();

    expect(screen.getByText("Airport Transfer")).toBeInTheDocument();
    expect(screen.getByText("Corporate Events")).toBeInTheDocument();
    expect(screen.queryByText("Wedding Planning")).not.toBeInTheDocument();
  });

  it("does not render a social links section when none are configured", () => {
    renderFooter();

    expect(screen.queryByText("Follow Us")).not.toBeInTheDocument();
  });
});
