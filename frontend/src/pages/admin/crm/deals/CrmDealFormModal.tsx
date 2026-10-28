import { useState, type FormEvent } from "react";
import Modal from "../../../../components/Modal";
import type { AdministratorSummaryDto } from "../../../../types/administrator";
import type { CrmCompanyDto } from "../../../../types/crmCompany";
import type { CrmDealDto } from "../../../../types/crmDeal";

export interface CrmDealFormValues {
  name: string;
  companyId: string;
  description: string;
  dealType: string;
  value: string;
  currency: string;
  expectedCloseDate: string;
  ownerId: string;
  nextAction: string;
  notes: string;
}

interface CrmDealFormModalProps {
  deal?: CrmDealDto;
  companies: CrmCompanyDto[];
  administrators: AdministratorSummaryDto[];
  defaultCompanyId?: string;
  onSave: (values: CrmDealFormValues) => Promise<void>;
  onClose: () => void;
}

function toFormValues(deal?: CrmDealDto, defaultCompanyId?: string): CrmDealFormValues {
  return {
    name: deal?.name ?? "",
    companyId: deal?.company.id ?? defaultCompanyId ?? "",
    description: deal?.description ?? "",
    dealType: deal?.dealType ?? "",
    value: deal ? String(deal.value) : "",
    currency: deal?.currency ?? "CHF",
    expectedCloseDate: deal?.expectedCloseDate ?? "",
    ownerId: deal?.owner?.id ?? "",
    nextAction: deal?.nextAction ?? "",
    notes: deal?.notes ?? "",
  };
}

function validate(values: CrmDealFormValues, isEdit: boolean): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!values.name.trim()) errors.name = "Deal name is required.";
  if (!isEdit && !values.companyId) errors.companyId = "Company is required.";
  if (!values.value.trim() || Number.isNaN(Number(values.value)) || Number(values.value) < 0) {
    errors.value = "Enter a valid, non-negative value.";
  }
  if (!values.currency.trim()) errors.currency = "Currency is required.";
  return errors;
}

function CrmDealFormModal({ deal, companies, administrators, defaultCompanyId, onSave, onClose }: CrmDealFormModalProps) {
  const [values, setValues] = useState<CrmDealFormValues>(toFormValues(deal, defaultCompanyId));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isEdit = Boolean(deal);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationErrors = validate(values, isEdit);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSubmitError(null);
    setIsSaving(true);
    try {
      await onSave(values);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : "Failed to save the deal.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? "Edit Deal" : "Add Deal"} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="crm-deal-name">Deal name</label>
          <br />
          <input id="crm-deal-name" type="text" value={values.name} onChange={(e) => setValues({ ...values, name: e.target.value })} />
          {errors.name && <p className="form-error">{errors.name}</p>}
        </div>

        {!isEdit && (
          <div>
            <label htmlFor="crm-deal-company">Company</label>
            <br />
            <select id="crm-deal-company" value={values.companyId} onChange={(e) => setValues({ ...values, companyId: e.target.value })}>
              <option value="">Select a company...</option>
              {companies.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
            {errors.companyId && <p className="form-error">{errors.companyId}</p>}
          </div>
        )}

        <div className="row">
          <div>
            <label htmlFor="crm-deal-value">Value</label>
            <br />
            <input id="crm-deal-value" type="number" min="0" step="0.01" value={values.value} onChange={(e) => setValues({ ...values, value: e.target.value })} />
            {errors.value && <p className="form-error">{errors.value}</p>}
          </div>
          <div>
            <label htmlFor="crm-deal-currency">Currency</label>
            <br />
            <input id="crm-deal-currency" type="text" value={values.currency} onChange={(e) => setValues({ ...values, currency: e.target.value.toUpperCase() })} maxLength={3} />
            {errors.currency && <p className="form-error">{errors.currency}</p>}
          </div>
          <div>
            <label htmlFor="crm-deal-closedate">Expected close date</label>
            <br />
            <input id="crm-deal-closedate" type="date" value={values.expectedCloseDate} onChange={(e) => setValues({ ...values, expectedCloseDate: e.target.value })} />
          </div>
        </div>

        <div className="row">
          <div>
            <label htmlFor="crm-deal-type">Deal type</label>
            <br />
            <input id="crm-deal-type" type="text" placeholder="e.g. Annual contract" value={values.dealType} onChange={(e) => setValues({ ...values, dealType: e.target.value })} />
          </div>
          <div>
            <label htmlFor="crm-deal-owner">Owner</label>
            <br />
            <select id="crm-deal-owner" value={values.ownerId} onChange={(e) => setValues({ ...values, ownerId: e.target.value })}>
              <option value="">Unassigned</option>
              {administrators.map((admin) => (
                <option key={admin.id} value={admin.id}>
                  {admin.firstName} {admin.lastName}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div>
          <label htmlFor="crm-deal-description">Description</label>
          <br />
          <textarea id="crm-deal-description" rows={2} value={values.description} onChange={(e) => setValues({ ...values, description: e.target.value })} />
        </div>

        <div>
          <label htmlFor="crm-deal-nextaction">Next action</label>
          <br />
          <input id="crm-deal-nextaction" type="text" value={values.nextAction} onChange={(e) => setValues({ ...values, nextAction: e.target.value })} />
        </div>

        <div>
          <label htmlFor="crm-deal-notes">Notes</label>
          <br />
          <textarea id="crm-deal-notes" rows={3} value={values.notes} onChange={(e) => setValues({ ...values, notes: e.target.value })} />
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

export default CrmDealFormModal;
