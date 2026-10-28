import { useState, type FormEvent } from "react";
import Modal from "../../../../components/Modal";
import { CRM_LIFECYCLE_STATUSES } from "../../../../types/crmCompany";
import type { AdministratorSummaryDto } from "../../../../types/administrator";
import type { CrmContactDto } from "../../../../types/crmContact";

export interface CrmContactFormValues {
  firstName: string;
  lastName: string;
  email: string;
  jobTitle: string;
  phone: string;
  preferredContactMethod: string;
  status: string;
  assignedOwnerId: string;
  notes: string;
}

interface CrmContactFormModalProps {
  contact?: CrmContactDto;
  administrators: AdministratorSummaryDto[];
  onSave: (values: CrmContactFormValues) => Promise<void>;
  onClose: () => void;
}

function toFormValues(contact?: CrmContactDto): CrmContactFormValues {
  return {
    firstName: contact?.firstName ?? "",
    lastName: contact?.lastName ?? "",
    email: contact?.email ?? "",
    jobTitle: contact?.jobTitle ?? "",
    phone: contact?.phone ?? "",
    preferredContactMethod: contact?.preferredContactMethod ?? "",
    status: contact?.status ?? "NewProspect",
    assignedOwnerId: contact?.assignedOwner?.id ?? "",
    notes: contact?.notes ?? "",
  };
}

function validate(values: CrmContactFormValues): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!values.firstName.trim()) errors.firstName = "First name is required.";
  if (!values.lastName.trim()) errors.lastName = "Last name is required.";
  if (!values.email.trim()) errors.email = "Email is required.";
  else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(values.email.trim())) errors.email = "Enter a valid email address.";
  return errors;
}

function CrmContactFormModal({ contact, administrators, onSave, onClose }: CrmContactFormModalProps) {
  const [values, setValues] = useState<CrmContactFormValues>(toFormValues(contact));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isEdit = Boolean(contact);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationErrors = validate(values);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSubmitError(null);
    setIsSaving(true);
    try {
      await onSave(values);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : "Failed to save the contact.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? "Edit Contact" : "Add Contact"} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <div className="row">
          <div>
            <label htmlFor="crm-contact-firstname">First name</label>
            <br />
            <input id="crm-contact-firstname" type="text" value={values.firstName} onChange={(e) => setValues({ ...values, firstName: e.target.value })} />
            {errors.firstName && <p className="form-error">{errors.firstName}</p>}
          </div>
          <div>
            <label htmlFor="crm-contact-lastname">Last name</label>
            <br />
            <input id="crm-contact-lastname" type="text" value={values.lastName} onChange={(e) => setValues({ ...values, lastName: e.target.value })} />
            {errors.lastName && <p className="form-error">{errors.lastName}</p>}
          </div>
        </div>

        <div>
          <label htmlFor="crm-contact-email">Email</label>
          <br />
          <input id="crm-contact-email" type="email" value={values.email} onChange={(e) => setValues({ ...values, email: e.target.value })} />
          {errors.email && <p className="form-error">{errors.email}</p>}
        </div>

        <div className="row">
          <div>
            <label htmlFor="crm-contact-jobtitle">Job title</label>
            <br />
            <input id="crm-contact-jobtitle" type="text" value={values.jobTitle} onChange={(e) => setValues({ ...values, jobTitle: e.target.value })} />
          </div>
          <div>
            <label htmlFor="crm-contact-phone">Phone</label>
            <br />
            <input id="crm-contact-phone" type="text" value={values.phone} onChange={(e) => setValues({ ...values, phone: e.target.value })} />
          </div>
        </div>

        <div className="row">
          <div>
            <label htmlFor="crm-contact-method">Preferred contact method</label>
            <br />
            <select id="crm-contact-method" value={values.preferredContactMethod} onChange={(e) => setValues({ ...values, preferredContactMethod: e.target.value })}>
              <option value="">No preference</option>
              <option value="Phone">Phone</option>
              <option value="Email">Email</option>
            </select>
          </div>
          <div>
            <label htmlFor="crm-contact-status">Status</label>
            <br />
            <select id="crm-contact-status" value={values.status} onChange={(e) => setValues({ ...values, status: e.target.value })}>
              {CRM_LIFECYCLE_STATUSES.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div>
          <label htmlFor="crm-contact-owner">Assigned owner</label>
          <br />
          <select id="crm-contact-owner" value={values.assignedOwnerId} onChange={(e) => setValues({ ...values, assignedOwnerId: e.target.value })}>
            <option value="">Unassigned</option>
            {administrators.map((admin) => (
              <option key={admin.id} value={admin.id}>
                {admin.firstName} {admin.lastName}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label htmlFor="crm-contact-notes">Notes</label>
          <br />
          <textarea id="crm-contact-notes" rows={3} value={values.notes} onChange={(e) => setValues({ ...values, notes: e.target.value })} />
        </div>

        {submitError && <p role="alert">{submitError}</p>}

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

export default CrmContactFormModal;
