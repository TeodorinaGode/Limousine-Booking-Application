import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ComposeEmailModal from "./ComposeEmailModal";
import * as crmEmailSendService from "../services/crmEmailSendService";
import * as crmEmailTemplateService from "../services/crmEmailTemplateService";
import * as authContext from "../context/AuthContext";

vi.mock("../services/crmEmailSendService");
vi.mock("../services/crmEmailTemplateService");
vi.mock("../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedSendService = vi.mocked(crmEmailSendService);
const mockedTemplateService = vi.mocked(crmEmailTemplateService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

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
  mockedTemplateService.getCrmEmailTemplates.mockResolvedValue({ items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 });
});

describe("ComposeEmailModal", () => {
  it("rejects sending with no recipient", async () => {
    const user = userEvent.setup();
    render(<ComposeEmailModal onClose={vi.fn()} onSent={vi.fn()} />);

    await user.type(screen.getByLabelText("Message"), "Hello");
    await user.click(screen.getByRole("button", { name: "Send" }));

    expect(await screen.findByText("At least one recipient is required.")).toBeInTheDocument();
    expect(mockedSendService.sendCrmEmail).not.toHaveBeenCalled();
  });

  it("sends the email and reports success when the provider confirms it", async () => {
    const user = userEvent.setup();
    mockedSendService.sendCrmEmail.mockResolvedValue({ status: "Accepted", messageId: "msg-1" });
    const onSent = vi.fn();

    render(<ComposeEmailModal prefill={{ toAddresses: [{ address: "client@example.com", name: "Jane" }], subject: "Hi" }} onClose={vi.fn()} onSent={onSent} />);

    await user.type(screen.getByLabelText("Message"), "Here is your quote.");
    await user.click(screen.getByRole("button", { name: "Send" }));

    await waitFor(() => expect(mockedSendService.sendCrmEmail).toHaveBeenCalledTimes(1));
    expect(await screen.findByText("Email sent.")).toBeInTheDocument();
    expect(onSent).toHaveBeenCalled();
  });

  it("never claims success on an unconfirmed (ambiguous) result, and offers a retry that reuses the same clientOperationId", async () => {
    const user = userEvent.setup();
    mockedSendService.sendCrmEmail.mockResolvedValue({ status: "Submitting", messageId: null });

    render(<ComposeEmailModal prefill={{ toAddresses: [{ address: "client@example.com", name: null }] }} onClose={vi.fn()} onSent={vi.fn()} />);

    await user.type(screen.getByLabelText("Message"), "Here is your quote.");
    await user.click(screen.getByRole("button", { name: "Send" }));

    expect(await screen.findByText(/could not be confirmed/)).toBeInTheDocument();
    expect(screen.queryByText("Email sent.")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Retry Send" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Retry Send" }));

    const [firstCallClientOperationId] = mockedSendService.sendCrmEmail.mock.calls[0];
    const [secondCallClientOperationId] = mockedSendService.sendCrmEmail.mock.calls[1];
    expect(secondCallClientOperationId).toBe(firstCallClientOperationId);
  });

  it("shows the server's error message on a definite failure", async () => {
    const user = userEvent.setup();
    mockedSendService.sendCrmEmail.mockRejectedValue(new Error("No connected email account is available to send from."));

    render(<ComposeEmailModal prefill={{ toAddresses: [{ address: "client@example.com", name: null }] }} onClose={vi.fn()} onSent={vi.fn()} />);

    await user.type(screen.getByLabelText("Message"), "Hello");
    await user.click(screen.getByRole("button", { name: "Send" }));

    expect(await screen.findByText("No connected email account is available to send from.")).toBeInTheDocument();
  });

  it("blocks sending while a template still has unresolved placeholders, and clears the block once the user edits the body", async () => {
    const user = userEvent.setup();
    mockedTemplateService.getCrmEmailTemplates.mockResolvedValue({
      items: [{ id: "template-1", name: "Quote Follow-Up", subject: "Hi", bodyHtml: "<p>Hi</p>", category: "General", language: "en", isArchived: false, createdAt: "", updatedAt: "" }],
      page: 1,
      pageSize: 100,
      totalCount: 1,
      totalPages: 1,
    });
    mockedTemplateService.renderCrmEmailTemplate.mockResolvedValue({
      subject: "Hi",
      bodyHtml: "<p>Dear {{contact.firstName}},</p>",
      unresolvedVariables: ["contact.firstName"],
    });

    render(<ComposeEmailModal prefill={{ toAddresses: [{ address: "client@example.com", name: null }] }} onClose={vi.fn()} onSent={vi.fn()} />);

    await user.selectOptions(await screen.findByLabelText("Insert a template"), "template-1");
    await user.click(screen.getByRole("button", { name: "Insert" }));
    expect(await screen.findByText(/unresolved placeholders/)).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Send" }));
    expect(await screen.findByText(/Resolve the template's remaining placeholders/)).toBeInTheDocument();
    expect(mockedSendService.sendCrmEmail).not.toHaveBeenCalled();

    await user.type(screen.getByLabelText("Message"), " Jane,");
    mockedSendService.sendCrmEmail.mockResolvedValue({ status: "Accepted", messageId: "msg-1" });
    await user.click(screen.getByRole("button", { name: "Send" }));

    await waitFor(() => expect(mockedSendService.sendCrmEmail).toHaveBeenCalledTimes(1));
  });
});
