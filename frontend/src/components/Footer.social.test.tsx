import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import Footer from "./Footer";
import * as companyService from "../services/companyService";

vi.mock("../services/companyService");

/**
 * Kept in its own file, separate from Footer.test.tsx, because
 * useCompanyInfo's module-level cache is populated by whichever mock is
 * active on the first render in a test file and does not change afterward —
 * mixing a "no social links" case and a "real social links" case in the same
 * file makes whichever runs second see the first case's cached value. A
 * fresh test file gets a fresh module registry, so this test always sees its
 * own mock.
 */
describe("Footer social links", () => {
  it("renders only the real, configured social links", async () => {
    vi.mocked(companyService).getCompanyInfo.mockResolvedValue({
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
      facebookUrl: "https://www.facebook.com/roi.limousinen",
      instagramUrl: "https://www.instagram.com/roi.limousinen/",
      whatsAppUrl: null,
    });

    render(
      <MemoryRouter>
        <Footer />
      </MemoryRouter>
    );

    expect(await screen.findByRole("link", { name: "Facebook" })).toHaveAttribute("href", "https://www.facebook.com/roi.limousinen");
    expect(screen.getByRole("link", { name: "Instagram" })).toHaveAttribute("href", "https://www.instagram.com/roi.limousinen/");
    expect(screen.queryByRole("link", { name: "WhatsApp" })).not.toBeInTheDocument();
  });
});
