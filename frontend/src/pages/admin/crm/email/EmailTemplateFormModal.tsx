import { useState } from "react";
import Modal from "../../../../components/Modal";
import type { CrmEmailTemplateDto } from "../../../../types/crmEmailTemplate";

export interface CrmEmailTemplateFormValues {
  name: string;
  subject: string;
  bodyHtml: string;
  category: string;
}

interface EmailTemplateFormModalProps {
  template?: CrmEmailTemplateDto;
  onSave: (values: CrmEmailTemplateFormValues) => Promise<void>;
  onClose: () => void;
}

/** {{contact.firstName}}, {{contact.lastName}}, {{contact.fullName}}, {{contact.email}}, {{company.name}}, {{deal.name}}, {{deal.value}}, {{deal.currency}}, {{owner.firstName}}, {{owner.lastName}}, {{owner.email}} */
const SUPPORTED_VARIABLES = [
  "contact.firstName",
  "contact.lastName",
  "contact.fullName",
  "contact.email",
  "company.name",
  "deal.name",
  "deal.value",
  "deal.currency",
  "owner.firstName",
  "owner.lastName",
  "owner.email",
];

function EmailTemplateFormModal({ template, onSave, onClose }: EmailTemplateFormModalProps) {
  const [name, setName] = useState(template?.name ?? "");
  const [category, setCategory] = useState(template?.category ?? "General");
  const [subject, setSubject] = useState(template?.subject ?? "");
  const [bodyHtml, setBodyHtml] = useState(template?.bodyHtml ?? "");
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setIsSaving(true);
    setError(null);
    try {
      await onSave({ name, subject, bodyHtml, category });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save the template.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title={template ? "Edit Template" : "New Template"} onClose={onClose}>
      <form onSubmit={handleSubmit} className="stack" style={{ minWidth: 420 }}>
        {error && <p role="alert">{error}</p>}

        <label>
          Name
          <input type="text" value={name} onChange={(e) => setName(e.target.value)} required />
        </label>
        <label>
          Category
          <input type="text" value={category} onChange={(e) => setCategory(e.target.value)} />
        </label>
        <label>
          Subject
          <input type="text" value={subject} onChange={(e) => setSubject(e.target.value)} required />
        </label>
        <label>
          Body
          <textarea value={bodyHtml} onChange={(e) => setBodyHtml(e.target.value)} rows={10} required />
        </label>
        <p style={{ fontSize: "0.85rem", color: "var(--color-text-secondary)" }}>
          Variables: {SUPPORTED_VARIABLES.map((v) => `{{${v}}}`).join(", ")}
        </p>

        <div style={{ marginTop: "1rem" }}>
          <button type="button" onClick={onClose} disabled={isSaving}>
            Cancel
          </button>{" "}
          <button type="submit" disabled={isSaving}>
            {isSaving ? "Saving..." : "Save"}
          </button>
        </div>
      </form>
    </Modal>
  );
}

export default EmailTemplateFormModal;
