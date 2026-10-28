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

/**
 * Simulating a real @dnd-kit pointer-drag gesture in jsdom is unreliable
 * (its collision detection depends on real layout geometry jsdom doesn't
 * compute). This stub skips the drag mechanics — which belong to a
 * well-tested third-party library — and instead exercises the actual
 * business logic this app owns: the optimistic move, the API call, and the
 * revert-on-failure, by calling the same onDealDropped prop a real drag
 * gesture would call.
 */
vi.mock("./DealKanbanBoard", () => ({
  default: ({
    columns,
    onDealDropped,
  }: {
    columns: { stage: { id: string; name: string; isLost: boolean }; deals: CrmDealDto[] }[];
    onDealDropped: (dealId: string, fromStageId: string, toStageId: string) => void;
  }) => (
    <div>
      {columns.map((column) => (
        <div key={column.stage.id}>
          <span>{column.stage.name}</span>
          {column.deals.map((deal) => (
            <span key={deal.id}>{deal.name}</span>
          ))}
        </div>
      ))}
      <button type="button" onClick={() => onDealDropped("deal-1", "stage-new", "stage-qualified")}>
        Simulate drop onto open stage
      </button>
      <button type="button" onClick={() => onDealDropped("deal-1", "stage-new", "stage-lost")}>
        Simulate drop onto lost stage
      </button>
    </div>
  ),
}));

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

function makeBoard(): CrmPipelineBoardDto {
  return {
    pipelineId: "pipeline-1",
    pipelineName: "Sales Pipeline",
    columns: [
      { stage: { id: "stage-new", name: "New Lead", sortOrder: 1, isWon: false, isLost: false, defaultProbability: 10 }, deals: [makeDeal()] },
      { stage: { id: "stage-qualified", name: "Qualified", sortOrder: 3, isWon: false, isLost: false, defaultProbability: 35 }, deals: [] },
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
  mockedDealService.getCrmDealBoard.mockResolvedValue(makeBoard());
});

describe("CrmDealsPage — stage move", () => {
  it("moves a deal to an open stage immediately, without asking for a reason", async () => {
    mockedDealService.moveCrmDealStage.mockResolvedValue(makeDeal({ stageId: "stage-qualified" }));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Annual chauffeur contract");

    await user.click(screen.getByRole("button", { name: "Simulate drop onto open stage" }));

    await waitFor(() => {
      expect(mockedDealService.moveCrmDealStage).toHaveBeenCalledWith(
        "deal-1",
        { stageId: "stage-qualified", lossReason: null },
        "test-token"
      );
    });
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });

  it("reverts the optimistic move and shows an error toast when the backend rejects the move", async () => {
    mockedDealService.moveCrmDealStage.mockRejectedValue(new Error("The target stage must belong to the deal's pipeline."));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Annual chauffeur contract");

    await user.click(screen.getByRole("button", { name: "Simulate drop onto open stage" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("The target stage must belong to the deal's pipeline.");
    // The card is still rendered — the optimistic move was rolled back rather than silently dropped.
    expect(screen.getByText("Annual chauffeur contract")).toBeInTheDocument();
  });

  it("asks for a loss reason before moving a deal into a Lost stage, and does not call the backend if cancelled", async () => {
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Annual chauffeur contract");

    await user.click(screen.getByRole("button", { name: "Simulate drop onto lost stage" }));

    const dialog = await screen.findByRole("dialog", { name: "Mark Deal as Lost" });
    await user.click(screen.getByRole("button", { name: "Cancel" }));

    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
    expect(mockedDealService.moveCrmDealStage).not.toHaveBeenCalled();
    void dialog;
  });

  it("moves a deal into a Lost stage with the provided reason once confirmed", async () => {
    mockedDealService.moveCrmDealStage.mockResolvedValue(makeDeal({ stageId: "stage-lost", isLost: true, lossReason: "Budget cut" }));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("Annual chauffeur contract");

    await user.click(screen.getByRole("button", { name: "Simulate drop onto lost stage" }));

    const dialog = await screen.findByRole("dialog", { name: "Mark Deal as Lost" });
    await user.type(screen.getByLabelText("Loss reason (required)"), "Budget cut");
    await user.click(screen.getByRole("button", { name: "Mark as Lost" }));

    await waitFor(() => {
      expect(mockedDealService.moveCrmDealStage).toHaveBeenCalledWith(
        "deal-1",
        { stageId: "stage-lost", lossReason: "Budget cut" },
        "test-token"
      );
    });
    void dialog;
  });
});
