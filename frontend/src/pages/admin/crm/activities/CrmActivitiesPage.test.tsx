import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CrmActivitiesPage from "./CrmActivitiesPage";
import { ToastProvider } from "../../../../context/ToastContext";
import * as crmActivityService from "../../../../services/crmActivityService";
import * as authContext from "../../../../context/AuthContext";
import type { CrmActivityDto } from "../../../../types/crmActivity";
import type { PagedResult } from "../../../../types/api";

vi.mock("../../../../services/crmActivityService");
vi.mock("../../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedActivityService = vi.mocked(crmActivityService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeActivity(overrides: Partial<CrmActivityDto> = {}): CrmActivityDto {
  return {
    id: "activity-1",
    activityType: "Call",
    subject: "Discussed contract terms",
    description: null,
    occurredAt: "2026-01-01T10:00:00Z",
    loggedBy: { id: "u1", firstName: "Admin", lastName: "User", email: "admin@example.com" },
    companyId: "company-1",
    companyName: "Grand Hotel Zurich",
    contactId: null,
    contactName: null,
    dealId: null,
    dealName: null,
    outcome: null,
    createdAt: "2026-01-01T10:00:00Z",
    emailMessageId: null,
    emailThreadId: null,
    ...overrides,
  };
}

function pagedResult<T>(items: T[]): PagedResult<T> {
  return { items, page: 1, pageSize: 25, totalCount: items.length, totalPages: 1 };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <CrmActivitiesPage />
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
});

describe("CrmActivitiesPage", () => {
  it("renders the activity log", async () => {
    mockedActivityService.getCrmActivities.mockResolvedValue(pagedResult([makeActivity()]));

    renderPage();

    expect(await screen.findByText("Discussed contract terms")).toBeInTheDocument();
    expect(screen.getByText("Grand Hotel Zurich")).toBeInTheDocument();
  });

  it("shows an error message when loading fails", async () => {
    mockedActivityService.getCrmActivities.mockRejectedValue(new Error("Network error"));

    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent("Network error");
  });

  it("logs a new activity", async () => {
    mockedActivityService.getCrmActivities.mockResolvedValue(pagedResult([]));
    mockedActivityService.createCrmActivity.mockResolvedValue(makeActivity());
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Activity Logged Yet");

    await user.click(screen.getByRole("button", { name: "+ Log Activity" }));
    await user.type(screen.getByLabelText("Subject"), "Discussed contract terms");
    await user.click(screen.getByRole("button", { name: "Log Activity" }));

    await waitFor(() => {
      expect(mockedActivityService.createCrmActivity).toHaveBeenCalledWith(
        expect.objectContaining({ subject: "Discussed contract terms", activityType: "Call" }),
        "test-token"
      );
    });
    expect(await screen.findByText("Activity logged.")).toBeInTheDocument();
  });

  it("rejects logging an activity with an empty subject client-side", async () => {
    mockedActivityService.getCrmActivities.mockResolvedValue(pagedResult([]));
    const user = userEvent.setup();

    renderPage();
    await screen.findByText("No Activity Logged Yet");

    await user.click(screen.getByRole("button", { name: "+ Log Activity" }));
    await user.click(screen.getByRole("button", { name: "Log Activity" }));

    expect(await screen.findByText("Subject is required.")).toBeInTheDocument();
    expect(mockedActivityService.createCrmActivity).not.toHaveBeenCalled();
  });
});
