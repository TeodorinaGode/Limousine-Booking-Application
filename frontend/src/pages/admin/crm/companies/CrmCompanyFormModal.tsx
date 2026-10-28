import { useState, type FormEvent } from "react";
import Modal from "../../../../components/Modal";
import type { AdministratorSummaryDto } from "../../../../types/administrator";
import {
  CRM_COMPANY_TYPES,
  CRM_LIFECYCLE_STATUSES,
  CRM_PRIORITIES,
  type CrmCompanyDto,
} from "../../../../types/crmCompany";

export interface CrmCompanyFormValues {
  name: string;
  companyType: string;
  website: string;
  domain: string;
  industry: string;
  country: string;
  city: string;
  address: string;
  phone: string;
  email: string;
  status: string;
  priority: string;
  assignedOwnerId: string;
  leadSource: string;
  notes: string;
}

interface CrmCompanyFormModalProps {
  company?: CrmCompanyDto;
  administrators: AdministratorSummaryDto[];
  onSave: (values: CrmCompanyFormValues) => Promise<void>;
  onClose: () => void;
}

function toFormValues(company?: CrmCompanyDto): CrmCompanyFormValues {
  return {
    name: company?.name ?? "",
    companyType: company?.companyType ?? "HotelResort",
    website: company?.website ?? "",
    domain: company?.domain ?? "",
    industry: company?.industry ?? "",
    country: company?.country ?? "",
    city: company?.city ?? "",
    address: company?.address ?? "",
    phone: company?.phone ?? "",
    email: company?.email ?? "",
    status: company?.status ?? "NewProspect",
    priority: company?.priority ?? "Normal",
    assignedOwnerId: company?.assignedOwner?.id ?? "",
    leadSource: company?.leadSource ?? "",
    notes: company?.notes ?? "",
  };
}

function validate(values: CrmCompanyFormValues): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!values.name.trim()) errors.name = "Company name is required.";
  if (values.email.trim() && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(values.email.trim())) {
    errors.email = "Enter a valid email address.";
  }
  return errors;
}

function CrmCompanyFormModal({ company, administrators, onSave, onClose }: CrmCompanyFormModalProps) {
  const [values, setValues] = useState<CrmCompanyFormValues>(toFormValues(company));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isEdit = Boolean(company);

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
      setSubmitError(error instanceof Error ? error.message : "Failed to save the company.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? "Edit Company" : "Add Company"} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="crm-company-name">Company name</label>
          <br />
          <input id="crm-company-name" type="text" value={values.name} onChange={(e) => setValues({ ...values, name: e.target.value })} />
          {errors.name && <p className="form-error">{errors.name}</p>}
        </div>

        <div className="row">
          <div>
            <label htmlFor="crm-company-type">Type</label>
            <br />
            <select id="crm-company-type" value={values.companyType} onChange={(e) => setValues({ ...values, companyType: e.target.value })}>
              {CRM_COMPANY_TYPES.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label htmlFor="crm-company-status">Status</label>
            <br />
            <select id="crm-company-status" value={values.status} onChange={(e) => setValues({ ...values, status: e.target.value })}>
              {CRM_LIFECYCLE_STATUSES.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label htmlFor="crm-company-priority">Priority</label>
            <br />
            <select id="crm-company-priority" value={values.priority} onChange={(e) => setValues({ ...values, priority: e.target.value })}>
              {CRM_PRIORITIES.map((priority) => (
                <option key={priority} value={priority}>
                  {priority}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="row">
          <div>
            <label htmlFor="crm-company-city">City</label>
            <br />
            <input id="crm-company-city" type="text" value={values.city} onChange={(e) => setValues({ ...values, city: e.target.value })} />
          </div>
          <div>
            <label htmlFor="crm-company-country">Country</label>
            <br />
            <input id="crm-company-country" type="text" value={values.country} onChange={(e) => setValues({ ...values, country: e.target.value })} />
          </div>
        </div>

        <div>
          <label htmlFor="crm-company-address">Address</label>
          <br />
          <input id="crm-company-address" type="text" value={values.address} onChange={(e) => setValues({ ...values, address: e.target.value })} />
        </div>

        <div className="row">
          <div>
            <label htmlFor="crm-company-phone">Phone</label>
            <br />
            <input id="crm-company-phone" type="text" value={values.phone} onChange={(e) => setValues({ ...values, phone: e.target.value })} />
          </div>
          <div>
            <label htmlFor="crm-company-email">Email</label>
            <br />
            <input id="crm-company-email" type="email" value={values.email} onChange={(e) => setValues({ ...values, email: e.target.value })} />
            {errors.email && <p className="form-error">{errors.email}</p>}
          </div>
        </div>

        <div className="row">
          <div>
            <label htmlFor="crm-company-website">Website</label>
            <br />
            <input id="crm-company-website" type="text" value={values.website} onChange={(e) => setValues({ ...values, website: e.target.value })} />
          </div>
          <div>
            <label htmlFor="crm-company-industry">Industry</label>
            <br />
            <input id="crm-company-industry" type="text" value={values.industry} onChange={(e) => setValues({ ...values, industry: e.target.value })} />
          </div>
        </div>

        <div className="row">
          <div>
            <label htmlFor="crm-company-owner">Assigned owner</label>
            <br />
            <select id="crm-company-owner" value={values.assignedOwnerId} onChange={(e) => setValues({ ...values, assignedOwnerId: e.target.value })}>
              <option value="">Unassigned</option>
              {administrators.map((admin) => (
                <option key={admin.id} value={admin.id}>
                  {admin.firstName} {admin.lastName}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="crm-company-leadsource">Lead source</label>
            <br />
            <input id="crm-company-leadsource" type="text" value={values.leadSource} onChange={(e) => setValues({ ...values, leadSource: e.target.value })} />
          </div>
        </div>

        <div>
          <label htmlFor="crm-company-notes">Notes</label>
          <br />
          <textarea id="crm-company-notes" rows={3} value={values.notes} onChange={(e) => setValues({ ...values, notes: e.target.value })} />
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

export default CrmCompanyFormModal;
