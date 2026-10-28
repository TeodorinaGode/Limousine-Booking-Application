import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useAuth } from "../../../../context/AuthContext";
import { useToast } from "../../../../context/ToastContext";
import {
  disconnectEmailAccount,
  getEmailAuthorizationUrl,
  getEmailConnectionStatus,
} from "../../../../services/crmEmailConnectionService";
import AdminNav from "../../../../components/AdminNav";
import CrmNav from "../../../../components/CrmNav";
import ConfirmDialog from "../../../../components/ConfirmDialog";
import PageHeader from "../../../../components/PageHeader";
import StatusBadge from "../../../../components/StatusBadge";
import type { EmailConnectionStatusDto } from "../../../../types/crmEmailConnection";

function formatDateTime(iso: string | null): string {
  if (!iso) return "Never";
  return new Date(iso).toLocaleString(undefined, { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function EmailSettingsPage() {
  const { accessToken } = useAuth();
  const { showToast } = useToast();
  const [searchParams, setSearchParams] = useSearchParams();

  const [status, setStatus] = useState<EmailConnectionStatusDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isConnecting, setIsConnecting] = useState(false);
  const [isDisconnectConfirmOpen, setIsDisconnectConfirmOpen] = useState(false);
  const [isDisconnecting, setIsDisconnecting] = useState(false);

  const loadStatus = useCallback(async () => {
    if (!accessToken) return;
    setIsLoading(true);
    setError(null);
    try {
      setStatus(await getEmailConnectionStatus(accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the email connection status.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken]);

  useEffect(() => {
    loadStatus();
  }, [loadStatus]);

  // The OAuth callback redirects the browser back here with ?connected=true
  // or ?connected=false&error=... — consume it once, then clear the query
  // string so a page refresh doesn't replay the same toast.
  useEffect(() => {
    const connected = searchParams.get("connected");
    if (connected === null) return;

    if (connected === "true") {
      showToast("Email account connected successfully.");
    } else {
      showToast(searchParams.get("error") ?? "Failed to connect the email account.", "error");
    }

    setSearchParams({}, { replace: true });
    loadStatus();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleConnect = async () => {
    if (!accessToken) return;
    setIsConnecting(true);
    try {
      const { authorizationUrl } = await getEmailAuthorizationUrl(accessToken);
      window.location.href = authorizationUrl;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to start the connection.");
      setIsConnecting(false);
    }
  };

  const handleConfirmDisconnect = async () => {
    if (!accessToken) return;
    setIsDisconnecting(true);
    try {
      await disconnectEmailAccount(accessToken);
      showToast("Email account disconnected.");
      await loadStatus();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to disconnect the email account.");
    } finally {
      setIsDisconnecting(false);
      setIsDisconnectConfirmOpen(false);
    }
  };

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main app-main--narrow">
        <PageHeader title="Email Integration" description="Connect a business Gmail account to sync, read, and send real email from the CRM." />
        <CrmNav />

        {error && <p role="alert">{error}</p>}

        {isLoading ? (
          <div className="skeleton skeleton-line" style={{ height: 140 }} />
        ) : !status ? null : !status.isConfigured ? (
          <section className="card">
            <h2 style={{ marginTop: 0 }}>Not Configured</h2>
            <p>
              Email integration is not configured on this server yet. An administrator needs to create a Google Cloud OAuth
              client and set the following environment variables (or User Secrets) before anyone can connect an account:
            </p>
            <ul>
              <li>
                <code>GmailOAuth__ClientId</code>
              </li>
              <li>
                <code>GmailOAuth__ClientSecret</code>
              </li>
              <li>
                <code>GmailOAuth__RedirectUri</code> — must exactly match a redirect URI registered on the OAuth client (e.g.{" "}
                <code>http://localhost:5249/api/admin/crm/email/connection/callback</code> in development)
              </li>
            </ul>
          </section>
        ) : (
          <section className="card">
            <h2 style={{ marginTop: 0 }}>Connection Status</h2>
            {status.isConnected ? (
              <>
                <div className="row" style={{ marginBottom: "var(--space-3)" }}>
                  <StatusBadge status={status.status ?? "Connected"} />
                  <span className="badge">{status.provider}</span>
                </div>
                <p>Connected address: {status.emailAddress}</p>
                <p>Last synced: {formatDateTime(status.lastSyncedAt)}</p>
                {status.lastSyncError && <p role="alert">Last sync error: {status.lastSyncError}</p>}

                <div className="row" style={{ marginTop: "var(--space-4)" }}>
                  <button type="button" className="btn-secondary" onClick={handleConnect} disabled={isConnecting}>
                    {isConnecting ? "Redirecting to Google..." : "Reconnect"}
                  </button>
                  <button type="button" className="btn-danger" onClick={() => setIsDisconnectConfirmOpen(true)}>
                    Disconnect
                  </button>
                </div>
              </>
            ) : (
              <>
                <p>No email account is connected yet.</p>
                <button type="button" onClick={handleConnect} disabled={isConnecting}>
                  {isConnecting ? "Redirecting to Google..." : "Connect Gmail Account"}
                </button>
              </>
            )}
          </section>
        )}

        {isDisconnectConfirmOpen && (
          <ConfirmDialog
            title="Disconnect Email Account"
            message={`Disconnect ${status?.emailAddress}? The CRM will stop syncing new messages and you won't be able to send email until it's reconnected. Previously synced messages remain visible.`}
            confirmLabel="Disconnect"
            danger
            isBusy={isDisconnecting}
            onConfirm={handleConfirmDisconnect}
            onCancel={() => setIsDisconnectConfirmOpen(false)}
          />
        )}
      </main>
    </div>
  );
}

export default EmailSettingsPage;
