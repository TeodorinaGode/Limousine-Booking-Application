import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CrmOverviewPage from "./CrmOverviewPage";
import * as crmAnalyticsService from "../../../services/crmAnalyticsService";
import * as authContext from "../../../context/AuthContext";
import type { CrmDashboardDto } from "../../../types/crmAnalytics";

vi.mock("../../../services/crmAnalyticsService");
vi.mock("../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedAnalyticsService = vi.mocked(crmAnalyticsService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeDashboard(overrides: Partial<CrmDashboardDto> = {}): CrmDashboardDto {
  return {
    totalCompanies: 12,
    totalContacts: 20,
    companiesContactedThisMonth: 4,
    contactsAndCompaniesNeedingFollowUp: 2,
    openDeals: 5,
    wonDeals: 3,
    lostDeals: 1,
    openPipelineValue: 150000,
    weightedPipelineValue: 62000,
    currency: "CHF",
    tasksDueToday: 2,
    overdueTasks: 1,
    upcomingFollowUps: 3,
    recentActivities: [],
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <CrmOverviewPage />
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

describe("CrmOverviewPage", () => {
  it("renders the computed dashboard metrics from the backend, not hardcoded values", async () => {
    mockedAnalyticsService.getCrmDashboard.mockResolvedValue(makeDashboard());

    renderPage();

    expect(await screen.findByText("12")).toBeInTheDocument();
    expect(screen.getByText("20")).toBeInTheDocument();
    expect(screen.getByText("CHF 150,000")).toBeInTheDocument();
  });

  it("shows an error message when loading fails", async () => {
    mockedAnalyticsService.getCrmDashboard.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("shows an empty state when there is no recent activity", async () => {
    mockedAnalyticsService.getCrmDashboard.mockResolvedValue(makeDashboard({ recentActivities: [] }));

    renderPage();

    expect(await screen.findByText("No Activity Yet")).toBeInTheDocument();
  });
});
