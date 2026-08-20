import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import FAQPage from "./FAQPage";
import { AuthProvider } from "../../context/AuthContext";
import * as companyService from "../../services/companyService";

vi.mock("../../services/companyService");

const mockedCompanyService = vi.mocked(companyService);

function renderPage() {
  return render(
    <MemoryRouter>
      <AuthProvider>
        <FAQPage />
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

describe("FAQPage", () => {
  it("renders all eight questions as collapsed accordion items", () => {
    renderPage();

    expect(screen.getByText("How do I book a limousine?")).toBeInTheDocument();
    expect(screen.getByText("Do I need an account?")).toBeInTheDocument();
    expect(screen.getByText("Can I change my pickup address?")).toBeInTheDocument();
  });

  it("reveals the answer when a question's summary is activated", async () => {
    const user = userEvent.setup();
    renderPage();

    const question = screen.getByText("Do I need an account?");
    expect(question.closest("details")).not.toHaveAttribute("open");

    await user.click(question);

    expect(question.closest("details")).toHaveAttribute("open");
    expect(screen.getByText(/You can book, pay and receive your confirmation entirely as a guest\./)).toBeVisible();
  });
});
