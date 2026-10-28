import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import EmailSettingsPage from "./EmailSettingsPage";
import { ToastProvider } from "../../../../context/ToastContext";
import * as crmEmailConnectionService from "../../../../services/crmEmailConnectionService";
import * as authContext from "../../../../context/AuthContext";
import type { EmailConnectionStatusDto } from "../../../../types/crmEmailConnection";

vi.mock("../../../../services/crmEmailConnectionService");
vi.mock("../../../../context/AuthContext", async () => {
  const actual = await vi.importActual<typeof authContext>("../../../../context/AuthContext");
  return { ...actual, useAuth: vi.fn() };
});

const mockedConnectionService = vi.mocked(crmEmailConnectionService);
const mockedUseAuth = vi.mocked(authContext.useAuth);

function makeStatus(overrides: Partial<EmailConnectionStatusDto> = {}): EmailConnectionStatusDto {
  return {
    isConfigured: true,
    isConnected: false,
    provider: null,
    emailAddress: null,
    status: null,
    lastSyncedAt: null,
    lastSyncError: null,
    ...overrides,
  };
}

function renderPage(initialPath = "/admin/crm/inbox/settings") {
  return render(
    <MemoryRouter initialEntries={[initialPath]}>
      <ToastProvider>
        <EmailSettingsPage />
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

describe("EmailSettingsPage", () => {
  it("shows the honest not-configured state, with no Connect button", async () => {
    mockedConnectionService.getEmailConnectionStatus.mockResolvedValue(makeStatus({ isConfigured: false }));

    renderPage();

    expect(await screen.findByText("Not Configured")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Connect Gmail Account" })).not.toBeInTheDocument();
  });

  it("shows a Connect button when configured but not yet connected", async () => {
    mockedConnectionService.getEmailConnectionStatus.mockResolvedValue(makeStatus());

    renderPage();

    expect(await screen.findByRole("button", { name: "Connect Gmail Account" })).toBeInTheDocument();
  });

  it("shows connection details and Disconnect when connected", async () => {
    mockedConnectionService.getEmailConnectionStatus.mockResolvedValue(
      makeStatus({ isConnected: true, provider: "Gmail", emailAddress: "sales@roi-limousinen.ch", status: "Connected" })
    );

    renderPage();

    expect(await screen.findByText("sales@roi-limousinen.ch", { exact: false })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Disconnect" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reconnect" })).toBeInTheDocument();
  });

  it("shows a success toast and reloads status after a successful OAuth callback redirect", async () => {
    mockedConnectionService.getEmailConnectionStatus.mockResolvedValue(
      makeStatus({ isConnected: true, provider: "Gmail", emailAddress: "sales@roi-limousinen.ch", status: "Connected" })
    );

    renderPage("/admin/crm/inbox/settings?connected=true");

    expect(await screen.findByText("Email account connected successfully.")).toBeInTheDocument();
    await waitFor(() => expect(mockedConnectionService.getEmailConnectionStatus).toHaveBeenCalled());
  });

  it("shows the server's error message after a failed OAuth callback redirect", async () => {
    mockedConnectionService.getEmailConnectionStatus.mockResolvedValue(makeStatus());

    renderPage("/admin/crm/inbox/settings?connected=false&error=Google%20rejected%20the%20request.");

    expect(await screen.findByText("Google rejected the request.")).toBeInTheDocument();
  });

  it("disconnects the account when confirmed", async () => {
    mockedConnectionService.getEmailConnectionStatus.mockResolvedValue(
      makeStatus({ isConnected: true, provider: "Gmail", emailAddress: "sales@roi-limousinen.ch", status: "Connected" })
    );
    mockedConnectionService.disconnectEmailAccount.mockResolvedValue();
    const user = userEvent.setup();

    renderPage();
    await screen.findByRole("button", { name: "Disconnect" });

    await user.click(screen.getByRole("button", { name: "Disconnect" }));
    const dialog = await screen.findByRole("dialog", { name: "Disconnect Email Account" });
    await user.click(within(dialog).getByRole("button", { name: "Disconnect" }));

    await waitFor(() => {
      expect(mockedConnectionService.disconnectEmailAccount).toHaveBeenCalledWith("test-token");
    });
  });
});
