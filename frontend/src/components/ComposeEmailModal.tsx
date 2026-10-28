import { useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { sendCrmEmail } from "../services/crmEmailSendService";
import { getCrmEmailTemplates, renderCrmEmailTemplate } from "../services/crmEmailTemplateService";
import type { EmailAddressDto } from "../types/crmEmailThread";
import type { CrmEmailTemplateDto } from "../types/crmEmailTemplate";
import Modal from "./Modal";

export interface ComposeEmailPrefill {
  threadId?: string | null;
  toAddresses?: EmailAddressDto[];
  subject?: string;
  companyId?: string | null;
  contactId?: string | null;
  dealId?: string | null;
}

interface ComposeEmailModalProps {
  prefill?: ComposeEmailPrefill;
  onClose: () => void;
  onSent: () => void;
}

function parseAddressList(raw: string): EmailAddressDto[] {
  return raw
    .split(",")
    .map((part) => part.trim())
    .filter(Boolean)
    .map((address) => ({ address, name: null }));
}

/**
 * Reusable real-send composer — launchable from the Inbox (reply/new) and,
 * in a later phase, from a Company/Contact/Deal detail page's "Compose
 * Email" quick action. clientOperationId is generated exactly once per
 * modal instance and never regenerated on retry, so a failed/unconfirmed
 * send can be retried safely without the backend ever sending it twice.
 */
function ComposeEmailModal({ prefill, onClose, onSent }: ComposeEmailModalProps) {
  const { accessToken } = useAuth();
  const [clientOperationId] = useState(() => crypto.randomUUID());

  const [toInput, setToInput] = useState(() => (prefill?.toAddresses ?? []).map((a) => a.address).join(", "));
  const [ccInput, setCcInput] = useState("");
  const [bccInput, setBccInput] = useState("");
  const [subject, setSubject] = useState(prefill?.subject ?? "");
  const [bodyHtml, setBodyHtml] = useState("");

  const [status, setStatus] = useState<"draft" | "sending" | "sent" | "unconfirmed" | "failed">("draft");
  const [error, setError] = useState<string | null>(null);

  const [templates, setTemplates] = useState<CrmEmailTemplateDto[]>([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState("");
  const [unresolvedVariables, setUnresolvedVariables] = useState<string[]>([]);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      try {
        const result = await getCrmEmailTemplates({ pageSize: 100 }, accessToken);
        setTemplates(result.items);
      } catch {
        // Non-fatal — the template picker just stays empty.
      }
    })();
  }, [accessToken]);

  const isBusy = status === "sending";

  const handleInsertTemplate = async () => {
    if (!accessToken || !selectedTemplateId) return;
    try {
      const rendered = await renderCrmEmailTemplate(
        selectedTemplateId,
        { companyId: prefill?.companyId, contactId: prefill?.contactId, dealId: prefill?.dealId },
        accessToken
      );
      setSubject(rendered.subject);
      setBodyHtml(rendered.bodyHtml);
      setUnresolvedVariables(rendered.unresolvedVariables);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the template.");
    }
  };

  const handleSend = async () => {
    if (!accessToken) return;

    const toAddresses = parseAddressList(toInput);
    if (toAddresses.length === 0) {
      setError("At least one recipient is required.");
      return;
    }
    if (!bodyHtml.trim()) {
      setError("The message body is required.");
      return;
    }
    if (unresolvedVariables.length > 0) {
      setError(`Resolve the template's remaining placeholders before sending: ${unresolvedVariables.join(", ")}`);
      return;
    }

    setStatus("sending");
    setError(null);
    try {
      const result = await sendCrmEmail(
        clientOperationId,
        {
          threadId: prefill?.threadId ?? null,
          toAddresses,
          ccAddresses: ccInput.trim() ? parseAddressList(ccInput) : undefined,
          bccAddresses: bccInput.trim() ? parseAddressList(bccInput) : undefined,
          subject,
          bodyHtml,
          companyId: prefill?.companyId ?? null,
          contactId: prefill?.contactId ?? null,
          dealId: prefill?.dealId ?? null,
        },
        accessToken
      );

      if (result.status === "Accepted") {
        setStatus("sent");
        onSent();
      } else {
        // HTTP 202 — the provider call itself was ambiguous. Keep the same
        // clientOperationId so "Retry" reconciles instead of double-sending.
        setStatus("unconfirmed");
        setError("The send result could not be confirmed. You can safely retry — it will not be sent twice.");
      }
    } catch (err) {
      setStatus("failed");
      setError(err instanceof Error ? err.message : "Failed to send the email.");
    }
  };

  return (
    <Modal title={prefill?.threadId ? "Reply" : "Compose Email"} onClose={onClose}>
      {status === "sent" ? (
        <div>
          <p role="status">Email sent.</p>
          <button type="button" onClick={onClose}>
            Close
          </button>
        </div>
      ) : (
        <div className="stack" style={{ minWidth: 420 }}>
          {error && <p role="alert">{error}</p>}

          <label>
            To
            <input type="text" value={toInput} onChange={(e) => setToInput(e.target.value)} placeholder="name@example.com, name2@example.com" disabled={isBusy} />
          </label>
          <label>
            Cc
            <input type="text" value={ccInput} onChange={(e) => setCcInput(e.target.value)} disabled={isBusy} />
          </label>
          <label>
            Bcc
            <input type="text" value={bccInput} onChange={(e) => setBccInput(e.target.value)} disabled={isBusy} />
          </label>
          {templates.length > 0 && (
            <div className="row">
              <select value={selectedTemplateId} onChange={(e) => setSelectedTemplateId(e.target.value)} aria-label="Insert a template" disabled={isBusy}>
                <option value="">Insert a template...</option>
                {templates.map((template) => (
                  <option key={template.id} value={template.id}>
                    {template.name}
                  </option>
                ))}
              </select>
              <button type="button" className="btn-secondary" disabled={!selectedTemplateId || isBusy} onClick={handleInsertTemplate}>
                Insert
              </button>
            </div>
          )}

          {unresolvedVariables.length > 0 && (
            <p role="alert">This template still has unresolved placeholders: {unresolvedVariables.map((v) => `{{${v}}}`).join(", ")}</p>
          )}

          <label>
            Subject
            <input
              type="text"
              value={subject}
              onChange={(e) => {
                setSubject(e.target.value);
                setUnresolvedVariables([]);
              }}
              disabled={isBusy}
            />
          </label>
          <label>
            Message
            <textarea
              value={bodyHtml}
              onChange={(e) => {
                setBodyHtml(e.target.value);
                setUnresolvedVariables([]);
              }}
              rows={10}
              disabled={isBusy}
            />
          </label>

          <div style={{ marginTop: "1rem" }}>
            <button type="button" onClick={onClose} disabled={isBusy}>
              Cancel
            </button>{" "}
            <button type="button" onClick={handleSend} disabled={isBusy}>
              {isBusy ? "Sending..." : status === "unconfirmed" || status === "failed" ? "Retry Send" : "Send"}
            </button>
          </div>
        </div>
      )}
    </Modal>
  );
}

export default ComposeEmailModal;
