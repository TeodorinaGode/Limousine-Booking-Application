import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CrmDealsPage from "./CrmDealsPage";
import { ToastProvider } from "../../../../context/ToastContext";
import * as crmDealService from "../../../../services/crmDealService";
import * as crmCompanyService from "../../../../services/crmCompanyService";
import * as administratorService from "../../../../services/administratorService";
import * as authContext from "../../../../context/AuthContext";
import type { CrmDealDto, CrmPipelineBoardDto } from "../../../../types/crmDeal";
import type { PagedResult } from "../../../../types/api";

vi.mock("../../../../services/crmDealService");
vi.mock("../../../../services/crmCompanyService");
vi.mock("../../../../services/administratorService");
vi.mock("../../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedDealService = vi.mocked(crmDealService);
const mockedCompanyService = vi.mocked(crmCompanyService);
const mockedAdministratorService = vi.mocked(administratorService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeDeal(overrides: Partial<CrmDealDto> = {}): CrmDealDto {
  return {
    id: "deal-1",
    name: "Annual chauffeur contract",
    company: { id: "company-1", name: "Grand Hotel Zurich", companyType: "HotelResort" },
    description: null,
    dealType: null,
    value: 50000,
    currency: "CHF",
    pipelineId: "pipeline-1",
    stageId: "stage-new",
    stageName: "New Lead",
    isWon: false,
    isLost: false,
    probability: 10,
    expectedCloseDate: null,
    owner: null,
    nextAction: null,
    closedAt: null,
    lossReason: null,
    notes: null,
    isArchived: false,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function makeBoard(deals: CrmDealDto[] = [makeDeal()]): CrmPipelineBoardDto {
  return {
    pipelineId: "pipeline-1",
    pipelineName: "Sales Pipeline",
    columns: [
      { stage: { id: "stage-new", name: "New Lead", sortOrder: 1, isWon: false, isLost: false, defaultProbability: 10 }, deals },
      { stage: { id: "stage-lost", name: "Closed Lost", sortOrder: 9, isWon: false, isLost: true, defaultProbability: 0 }, deals: [] },
    ],
  };
}

function pagedResult<T>(items: T[]): PagedResult<T> {
  return { items, page: 1, pageSize: 20, totalCount: items.length, totalPages: 1 };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <CrmDealsPage />
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
  mockedCompanyService.getCrmCompanies.mockResolvedValue(pagedResult([]));
  mockedAdministratorService.getAdministrators.mockResolvedValue([]);
});

describe("CrmDealsPage", () => {
  it("renders the Kanban board by default", async () => {
    mockedDealService.getCrmDealBoard.mockResolvedValue(makeBoard());

    renderPage();

    expect(await screen.findByText("Annual chauffeur contract")).toBeInTheDocument();
    expect(screen.getByText("New Lead")).toBeInTheDocument();
    expect(screen.getByText("Closed Lost")).toBeInTheDocument();
  });

  it("switches to the table view and loads deals via the backend", async () => {
    mockedDealService.getCrmDealBoard.mockResolvedValue(makeBoard());
    mockedDealService.getCrmDeals.mockResolvedValue(pagedResult([makeDeal()]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Annual chauffeur contract");

    await user.click(screen.getByRole("button", { name: "Table" }));

    await waitFor(() => {
      expect(mockedDealService.getCrmDeals).toHaveBeenCalled();
    });
    expect(await screen.findByText("Grand Hotel Zurich")).toBeInTheDocument();
  });

  it("rejects an empty create-deal form client-side", async () => {
    mockedDealService.getCrmDealBoard.mockResolvedValue(makeBoard([]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("New Lead");

    await user.click(screen.getByRole("button", { name: "+ New Deal" }));
    await user.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByText("Deal name is required.")).toBeInTheDocument();
    expect(mockedDealService.createCrmDeal).not.toHaveBeenCalled();
  });

  it("creates a deal", async () => {
    mockedDealService.getCrmDealBoard.mockResolvedValue(makeBoard([]));
    mockedCompanyService.getCrmCompanies.mockResolvedValue(
      pagedResult([{ id: "company-1", name: "Grand Hotel Zurich" } as never])
    );
    mockedDealService.createCrmDeal.mockResolvedValue(makeDeal());
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("New Lead");

    await user.click(screen.getByRole("button", { name: "+ New Deal" }));
    await user.type(screen.getByLabelText("Deal name"), "Annual chauffeur contract");
    await user.selectOptions(screen.getByLabelText("Company"), "company-1");
    const valueInput = screen.getByLabelText("Value");
    await user.clear(valueInput);
    await user.type(valueInput, "50000");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() => {
      expect(mockedDealService.createCrmDeal).toHaveBeenCalledWith(
        expect.objectContaining({ name: "Annual chauffeur contract", companyId: "company-1", value: 50000 }),
        "test-token"
      );
    });
    expect(await screen.findByText("Deal created successfully.")).toBeInTheDocument();
  });
});
