import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CrmInboxPage from "./CrmInboxPage";
import { ToastProvider } from "../../../../context/ToastContext";
import * as crmInboxService from "../../../../services/crmInboxService";
import * as crmCompanyService from "../../../../services/crmCompanyService";
import * as crmContactService from "../../../../services/crmContactService";
import * as crmDealService from "../../../../services/crmDealService";
import * as authContext from "../../../../context/AuthContext";
import type { CrmEmailThreadDetailDto, CrmEmailThreadListItemDto } from "../../../../types/crmEmailThread";
import type { CrmCompanyDto } from "../../../../types/crmCompany";

vi.mock("../../../../services/crmInboxService");
vi.mock("../../../../services/crmCompanyService");
vi.mock("../../../../services/crmContactService");
vi.mock("../../../../services/crmDealService");
vi.mock("../../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedInboxService = vi.mocked(crmInboxService);
const mockedCompanyService = vi.mocked(crmCompanyService);
const mockedContactService = vi.mocked(crmContactService);
const mockedDealService = vi.mocked(crmDealService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeThreadListItem(overrides: Partial<CrmEmailThreadListItemDto> = {}): CrmEmailThreadListItemDto {
  return {
    id: "thread-1",
    subject: "Quote request for a wedding transfer",
    lastMessageAt: "2026-10-01T10:00:00Z",
    hasUnread: true,
    latestFromAddress: "client@example.com",
    latestFromName: "Jane Client",
    associatedCompanyNames: [],
    associatedContactNames: [],
    associatedDealNames: [],
    ...overrides,
  };
}

function makeThreadDetail(overrides: Partial<CrmEmailThreadDetailDto> = {}): CrmEmailThreadDetailDto {
  return {
    id: "thread-1",
    subject: "Quote request for a wedding transfer",
    hasUnread: true,
    messages: [
      {
        id: "msg-1",
        direction: "Inbound",
        fromAddress: "client@example.com",
        fromName: "Jane Client",
        toAddresses: [{ address: "sales@roi-limousinen.ch", name: null }],
        ccAddresses: [],
        bccAddresses: [],
        subject: "Quote request for a wedding transfer",
        bodyHtml: "<p>Hello, could you send a quote?</p>",
        bodyPlainText: "Hello, could you send a quote?",
        occurredAt: "2026-10-01T10:00:00Z",
        isRead: false,
        attachments: [],
      },
    ],
    companies: [],
    contacts: [],
    deals: [],
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <CrmInboxPage />
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
  mockedCompanyService.getCrmCompanies.mockResolvedValue({ items: [], page: 1, pageSize: 200, totalCount: 0, totalPages: 0 });
  mockedContactService.getCrmContacts.mockResolvedValue({ items: [], page: 1, pageSize: 200, totalCount: 0, totalPages: 0 });
  mockedDealService.getCrmDeals.mockResolvedValue({ items: [], page: 1, pageSize: 200, totalCount: 0, totalPages: 0 });
});

describe("CrmInboxPage", () => {
  it("shows an empty state when there are no synced conversations", async () => {
    mockedInboxService.getCrmEmailThreads.mockResolvedValue({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 });

    renderPage();

    expect(await screen.findByText("No Conversations")).toBeInTheDocument();
  });

  it("lists synced conversations with sender, subject, and tags", async () => {
    mockedInboxService.getCrmEmailThreads.mockResolvedValue({
      items: [makeThreadListItem({ associatedCompanyNames: ["Grand Hotel"] })],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      totalPages: 1,
    });

    renderPage();

    expect(await screen.findByText("Quote request for a wedding transfer")).toBeInTheDocument();
    expect(screen.getByText("Jane Client")).toBeInTheDocument();
    expect(screen.getByText("Grand Hotel")).toBeInTheDocument();
  });

  it("opens a conversation and marks it read when it was unread", async () => {
    const user = userEvent.setup();
    mockedInboxService.getCrmEmailThreads.mockResolvedValue({
      items: [makeThreadListItem()],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      totalPages: 1,
    });
    mockedInboxService.getCrmEmailThreadById.mockResolvedValue(makeThreadDetail());
    mockedInboxService.markCrmEmailThreadRead.mockResolvedValue(makeThreadDetail({ hasUnread: false }));

    renderPage();
    await user.click(await screen.findByText("Quote request for a wedding transfer"));

    // The sanitized body renders inside a sandboxed iframe (srcDoc), which
    // jsdom doesn't expose to screen queries against the main document — so
    // assert on the srcDoc content itself rather than the rendered text.
    const frame = await screen.findByTitle("Email message body");
    expect(frame.getAttribute("srcdoc")).toContain("Hello, could you send a quote?");
    await waitFor(() => expect(mockedInboxService.markCrmEmailThreadRead).toHaveBeenCalledWith("thread-1", "test-token"));
    expect(await screen.findByRole("button", { name: "Mark as Unread" })).toBeInTheDocument();
  });

  it("links a company to the open conversation", async () => {
    const user = userEvent.setup();
    const company: CrmCompanyDto = {
      id: "company-1",
      name: "Grand Hotel",
      companyType: "HotelResort",
      website: null,
      domain: null,
      industry: null,
      country: null,
      city: null,
      address: null,
      phone: null,
      email: null,
      status: "Customer",
      priority: "Normal",
      assignedOwner: null,
      leadSource: null,
      notes: null,
      lastContactedAt: null,
      nextFollowUpAt: null,
      isArchived: false,
      createdAt: "2026-01-01T00:00:00Z",
      updatedAt: "2026-01-01T00:00:00Z",
    };
    mockedCompanyService.getCrmCompanies.mockResolvedValue({ items: [company], page: 1, pageSize: 200, totalCount: 1, totalPages: 1 });
    mockedInboxService.getCrmEmailThreads.mockResolvedValue({
      items: [makeThreadListItem({ hasUnread: false })],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      totalPages: 1,
    });
    mockedInboxService.getCrmEmailThreadById.mockResolvedValue(makeThreadDetail({ hasUnread: false }));
    mockedInboxService.linkCrmEmailThreadCompany.mockResolvedValue(
      makeThreadDetail({ hasUnread: false, companies: [{ id: "company-1", name: "Grand Hotel", companyType: "HotelResort" }] })
    );

    renderPage();
    await user.click(await screen.findByText("Quote request for a wedding transfer"));
    await screen.findByText("No company linked yet.");

    await user.selectOptions(screen.getByLabelText("Company to link"), "company-1");
    await user.click(screen.getByRole("button", { name: "Link Company" }));

    await waitFor(() => expect(mockedInboxService.linkCrmEmailThreadCompany).toHaveBeenCalledWith("thread-1", "company-1", "test-token"));
  });
});
