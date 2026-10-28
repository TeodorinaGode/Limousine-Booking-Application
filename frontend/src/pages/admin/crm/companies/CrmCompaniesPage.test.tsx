import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CrmCompaniesPage from "./CrmCompaniesPage";
import { ToastProvider } from "../../../../context/ToastContext";
import * as crmCompanyService from "../../../../services/crmCompanyService";
import * as administratorService from "../../../../services/administratorService";
import * as authContext from "../../../../context/AuthContext";
import type { CrmCompanyDto } from "../../../../types/crmCompany";
import type { PagedResult } from "../../../../types/api";

vi.mock("../../../../services/crmCompanyService");
vi.mock("../../../../services/administratorService");
vi.mock("../../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedCompanyService = vi.mocked(crmCompanyService);
const mockedAdministratorService = vi.mocked(administratorService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeCompany(overrides: Partial<CrmCompanyDto> = {}): CrmCompanyDto {
  return {
    id: "11111111-1111-1111-1111-111111111111",
    name: "Grand Hotel Zurich",
    companyType: "HotelResort",
    website: null,
    domain: null,
    industry: null,
    country: "Switzerland",
    city: "Zurich",
    address: null,
    phone: null,
    email: null,
    status: "NewProspect",
    priority: "Normal",
    assignedOwner: null,
    leadSource: null,
    lastContactedAt: null,
    nextFollowUpAt: null,
    notes: null,
    isArchived: false,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function pagedResult<T>(items: T[]): PagedResult<T> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <CrmCompaniesPage />
      </ToastProvider>
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
  mockedAdministratorService.getAdministrators.mockResolvedValue([]);
});

describe("CrmCompaniesPage", () => {
  it("renders the company list", async () => {
    mockedCompanyService.getCrmCompanies.mockResolvedValue(
      pagedResult([makeCompany(), makeCompany({ id: "2", name: "Lakeside Travel Agency", companyType: "TravelAgency" })])
    );

    renderPage();

    expect(await screen.findByText("Grand Hotel Zurich")).toBeInTheDocument();
    expect(screen.getByText("Lakeside Travel Agency")).toBeInTheDocument();
  });

  it("shows an error message when loading fails", async () => {
    mockedCompanyService.getCrmCompanies.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("searches companies via the backend, not client-side filtering", async () => {
    mockedCompanyService.getCrmCompanies.mockResolvedValue(pagedResult([makeCompany()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Grand Hotel Zurich");

    await user.type(screen.getByLabelText("Search companies"), "Grand");

    await waitFor(
      () => {
        expect(mockedCompanyService.getCrmCompanies).toHaveBeenLastCalledWith(
          expect.objectContaining({ search: "Grand" }),
          "test-token"
        );
      },
      { timeout: 1000 }
    );
  });

  it("rejects an empty create-company form client-side", async () => {
    mockedCompanyService.getCrmCompanies.mockResolvedValue(pagedResult([]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Companies Found");

    await user.click(screen.getByRole("button", { name: "+ New Company" }));
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("Company name is required.")).toBeInTheDocument();
    expect(mockedCompanyService.createCrmCompany).not.toHaveBeenCalled();
  });

  it("creates a company", async () => {
    mockedCompanyService.getCrmCompanies.mockResolvedValue(pagedResult([]));
    mockedCompanyService.createCrmCompany.mockResolvedValue(makeCompany());
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Companies Found");

    await user.click(screen.getByRole("button", { name: "+ New Company" }));
    await user.type(screen.getByLabelText("Company name"), "Grand Hotel Zurich");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(mockedCompanyService.createCrmCompany).toHaveBeenCalledWith(
        expect.objectContaining({ name: "Grand Hotel Zurich" }),
        "test-token"
      );
    });
    expect(await screen.findByRole("status")).toHaveTextContent("Company created successfully.");
  });

  it("does not archive a company when the confirmation is declined", async () => {
    mockedCompanyService.getCrmCompanies.mockResolvedValue(pagedResult([makeCompany()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Grand Hotel Zurich");

    await user.click(screen.getByRole("button", { name: "Archive" }));
    const dialog = await screen.findByRole("dialog", { name: "Archive Company" });
    await user.click(within(dialog).getByRole("button", { name: "Cancel" }));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(mockedCompanyService.archiveCrmCompany).not.toHaveBeenCalled();
  });

  it("archives a company when the confirmation is accepted", async () => {
    const company = makeCompany();
    mockedCompanyService.getCrmCompanies.mockResolvedValue(pagedResult([company]));
    mockedCompanyService.archiveCrmCompany.mockResolvedValue({ ...company, isArchived: true });
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Grand Hotel Zurich");

    await user.click(screen.getByRole("button", { name: "Archive" }));
    const dialog = await screen.findByRole("dialog", { name: "Archive Company" });
    await user.click(within(dialog).getByRole("button", { name: "Archive" }));

    await waitFor(() => {
      expect(mockedCompanyService.archiveCrmCompany).toHaveBeenCalledWith(company.id, "test-token");
    });
  });
});
