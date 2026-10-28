import { useState, type FormEvent } from "react";
import Modal from "./Modal";
import { CRM_ACTIVITY_TYPES, type CreateCrmActivityRequest } from "../types/crmActivity";

interface LogActivityModalProps {
  /** Pre-fills whichever association this modal was opened from — company, contact, and/or deal detail pages. */
  companyId?: string;
  contactId?: string;
  dealId?: string;
  onSave: (values: CreateCrmActivityRequest) => Promise<void>;
  onClose: () => void;
}

/** Launchable from the Activities page or from any Company/Contact/Deal detail page, pre-associating whichever record it was opened from. */
function LogActivityModal({ companyId, contactId, dealId, onSave, onClose }: LogActivityModalProps) {
  const [activityType, setActivityType] = useState<string>("Call");
  const [subject, setSubject] = useState("");
  const [description, setDescription] = useState("");
  const [outcome, setOutcome] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!subject.trim()) {
      setError("Subject is required.");
      return;
    }

    setError(null);
    setIsSaving(true);
    try {
      await onSave({
        activityType: activityType as CreateCrmActivityRequest["activityType"],
        subject: subject.trim(),
        description: description.trim() || null,
        outcome: outcome.trim() || null,
        companyId,
        contactId,
        dealId,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to log the activity.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title="Log Activity" onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="crm-activity-type">Type</label>
          <br />
          <select id="crm-activity-type" value={activityType} onChange={(e) => setActivityType(e.target.value)}>
            {CRM_ACTIVITY_TYPES.map((type) => (
              <option key={type} value={type}>
                {type}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label htmlFor="crm-activity-subject">Subject</label>
          <br />
          <input id="crm-activity-subject" type="text" value={subject} onChange={(e) => setSubject(e.target.value)} />
        </div>

        <div>
          <label htmlFor="crm-activity-description">Description</label>
          <br />
          <textarea id="crm-activity-description" rows={3} value={description} onChange={(e) => setDescription(e.target.value)} />
        </div>

        <div>
          <label htmlFor="crm-activity-outcome">Outcome</label>
          <br />
          <input id="crm-activity-outcome" type="text" value={outcome} onChange={(e) => setOutcome(e.target.value)} />
        </div>

        {error && <p role="alert">{error}</p>}

        <div style={{ marginTop: "1rem" }}>
          <button type="button" onClick={onClose} disabled={isSaving}>
            Cancel
          </button>{" "}
          <button type="submit" disabled={isSaving}>
            {isSaving ? "Saving..." : "Log Activity"}
          </button>
        </div>
      </form>
    </Modal>
  );
}

export default LogActivityModal;
