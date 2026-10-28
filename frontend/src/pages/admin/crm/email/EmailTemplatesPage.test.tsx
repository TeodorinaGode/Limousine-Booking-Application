import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import EmailTemplatesPage from "./EmailTemplatesPage";
import { ToastProvider } from "../../../../context/ToastContext";
import * as crmEmailTemplateService from "../../../../services/crmEmailTemplateService";
import * as authContext from "../../../../context/AuthContext";
import type { CrmEmailTemplateDto } from "../../../../types/crmEmailTemplate";

vi.mock("../../../../services/crmEmailTemplateService");
vi.mock("../../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedTemplateService = vi.mocked(crmEmailTemplateService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeTemplate(overrides: Partial<CrmEmailTemplateDto> = {}): CrmEmailTemplateDto {
  return {
    id: "template-1",
    name: "Quote Follow-Up",
    subject: "Following up on your quote",
    bodyHtml: "<p>Dear {{contact.firstName}},</p>",
    category: "Follow-Up",
    language: "en",
    isArchived: false,
    createdAt: "2026-01-01T00:00:00Z",
    updatedAt: "2026-01-01T00:00:00Z",
    ...overrides,
  };
}

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <EmailTemplatesPage />
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

describe("EmailTemplatesPage", () => {
  it("shows an empty state when there are no templates", async () => {
    mockedTemplateService.getCrmEmailTemplates.mockResolvedValue({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 });

    renderPage();

    expect(await screen.findByText("No Templates Found")).toBeInTheDocument();
  });

  it("lists templates with their category and subject", async () => {
    mockedTemplateService.getCrmEmailTemplates.mockResolvedValue({ items: [makeTemplate()], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 });

    renderPage();

    expect(await screen.findByText("Quote Follow-Up")).toBeInTheDocument();
    expect(screen.getByText("Follow-Up")).toBeInTheDocument();
    expect(screen.getByText("Following up on your quote")).toBeInTheDocument();
  });

  it("creates a new template", async () => {
    const user = userEvent.setup();
    mockedTemplateService.getCrmEmailTemplates.mockResolvedValue({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 0 });
    mockedTemplateService.createCrmEmailTemplate.mockResolvedValue(makeTemplate());

    renderPage();
    await screen.findByText("No Templates Found");

    await user.click(screen.getByRole("button", { name: "+ New Template" }));
    await user.type(screen.getByLabelText("Name"), "Quote Follow-Up");
    await user.type(screen.getByLabelText("Subject"), "Following up on your quote");
    await user.type(screen.getByLabelText("Body"), "Dear {{contact.firstName}},");
    await user.click(screen.getByRole("button", { name: "Save" }));

    await waitFor(() =>
      expect(mockedTemplateService.createCrmEmailTemplate).toHaveBeenCalledWith(
        expect.objectContaining({ name: "Quote Follow-Up", subject: "Following up on your quote" }),
        "test-token"
      )
    );
  });

  it("archives a template", async () => {
    const user = userEvent.setup();
    mockedTemplateService.getCrmEmailTemplates.mockResolvedValue({ items: [makeTemplate()], page: 1, pageSize: 25, totalCount: 1, totalPages: 1 });
    mockedTemplateService.archiveCrmEmailTemplate.mockResolvedValue(makeTemplate({ isArchived: true }));

    renderPage();
    await user.click(await screen.findByRole("button", { name: "Archive" }));

    await waitFor(() => expect(mockedTemplateService.archiveCrmEmailTemplate).toHaveBeenCalledWith("template-1", "test-token"));
  });
});
