import { useState, type FormEvent } from "react";
import Modal from "../../../components/Modal";
import type { DriverDto } from "../../../types/driver";

interface ResetPasswordModalProps {
  driver: DriverDto;
  onSave: (newPassword: string) => Promise<void>;
  onClose: () => void;
}

function ResetPasswordModal({ driver, onSave, onClose }: ResetPasswordModalProps) {
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (newPassword.length < 8) {
      setError("Password must be at least 8 characters.");
      return;
    }
    setError(null);

    setSubmitError(null);
    setIsSaving(true);
    try {
      await onSave(newPassword);
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : "Failed to reset the password.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title={`Reset Password — ${driver.firstName} ${driver.lastName}`} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="newPassword">New password</label>
          <br />
          <input
            id="newPassword"
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
          />
          {error && <p role="alert">{error}</p>}
        </div>

        {submitError && <p role="alert">{submitError}</p>}

        <div style={{ marginTop: "1rem" }}>
          <button type="button" onClick={onClose} disabled={isSaving}>
            Cancel
          </button>{" "}
          <button type="submit" disabled={isSaving}>
            {isSaving ? "Saving..." : "Reset Password"}
          </button>
        </div>
      </form>
    </Modal>
  );
}

export default ResetPasswordModal;
