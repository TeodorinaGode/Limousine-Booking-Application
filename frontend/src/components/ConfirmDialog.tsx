import { useState } from "react";
import type { ReactNode } from "react";
import Modal from "./Modal";

interface ConfirmDialogProps {
  title: string;
  message: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
  /** When set, shows an optional free-text reason field and passes its (trimmed, possibly undefined) value to onConfirm. */
  reasonLabel?: string;
  isBusy?: boolean;
  onConfirm: (reason?: string) => void;
  onCancel: () => void;
}

/** In-app replacement for window.confirm()/window.prompt() (section 60) — same Modal every other dialog uses, so destructive actions stay on-brand and keyboard/screen-reader accessible instead of dropping into native OS chrome. */
function ConfirmDialog({
  title,
  message,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  danger = false,
  reasonLabel,
  isBusy = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  const [reason, setReason] = useState("");

  return (
    <Modal title={title} onClose={onCancel}>
      <div>{message}</div>

      {reasonLabel && (
        <div className="form-group" style={{ marginTop: "var(--space-4)" }}>
          <label htmlFor="confirm-dialog-reason">{reasonLabel}</label>
          <br />
          <textarea id="confirm-dialog-reason" value={reason} onChange={(event) => setReason(event.target.value)} rows={3} />
        </div>
      )}

      <div className="row" style={{ marginTop: "var(--space-6)", justifyContent: "flex-end" }}>
        <button type="button" className="btn-secondary" onClick={onCancel} disabled={isBusy}>
          {cancelLabel}
        </button>
        <button
          type="button"
          className={danger ? "btn-danger" : undefined}
          onClick={() => onConfirm(reasonLabel ? (reason.trim() === "" ? undefined : reason.trim()) : undefined)}
          disabled={isBusy}
        >
          {isBusy ? "Working..." : confirmLabel}
        </button>
      </div>
    </Modal>
  );
}

export default ConfirmDialog;
