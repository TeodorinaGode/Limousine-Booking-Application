import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../../../context/AuthContext";
import { useToast } from "../../../../context/ToastContext";
import { getAdministrators } from "../../../../services/administratorService";
import {
  archiveCrmCompany,
  createCrmCompany,
  getCrmCompanies,
  unarchiveCrmCompany,
  updateCrmCompany,
} from "../../../../services/crmCompanyService";
import AdminNav from "../../../../components/AdminNav";
import ComposeEmailModal from "../../../../components/ComposeEmailModal";
import CrmNav from "../../../../components/CrmNav";
import ConfirmDialog from "../../../../components/ConfirmDialog";
import PageHeader from "../../../../components/PageHeader";
import StatusBadge from "../../../../components/StatusBadge";
import type { AdministratorSummaryDto } from "../../../../types/administrator";
import { CRM_COMPANY_TYPES, CRM_LIFECYCLE_STATUSES, type CrmCompanyDto } from "../../../../types/crmCompany";
import CrmCompanyFormModal, { type CrmCompanyFormValues } from "./CrmCompanyFormModal";

const PAGE_SIZE = 20;

function CrmCompaniesPage() {
  const { accessToken } = useAuth();
  const { showToast } = useToast();

  const [companies, setCompanies] = useState<CrmCompanyDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [typeFilter, setTypeFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [administrators, setAdministrators] = useState<AdministratorSummaryDto[]>([]);
  const [modalState, setModalState] = useState<{ company?: CrmCompanyDto } | null>(null);
  const [composeTarget, setComposeTarget] = useState<CrmCompanyDto | null>(null);
  const [archiveTarget, setArchiveTarget] = useState<CrmCompanyDto | null>(null);
  const [isArchiving, setIsArchiving] = useState(false);

  const loadCompanies = useCallback(async () => {
    if (!accessToken) return;
    setIsLoading(true);
    setError(null);
    try {
      const result = await getCrmCompanies(
        { search, companyType: typeFilter || undefined, status: statusFilter || undefined, page, pageSize: PAGE_SIZE },
        accessToken
      );
      setCompanies(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load companies.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, search, typeFilter, statusFilter, page]);

  const loadAdministrators = useCallback(async () => {
    if (!accessToken) return;
    try {
      setAdministrators(await getAdministrators(accessToken));
    } catch {
      // Non-fatal — the owner dropdown just falls back to "Unassigned" only.
    }
  }, [accessToken]);

  useEffect(() => {
    loadCompanies();
  }, [loadCompanies]);

  useEffect(() => {
    loadAdministrators();
  }, [loadAdministrators]);

  useEffect(() => {
    const timeout = setTimeout(() => {
      setPage(1);
      setSearch(searchInput);
    }, 300);
    return () => clearTimeout(timeout);
  }, [searchInput]);

  const handleSave = async (values: CrmCompanyFormValues) => {
    if (!accessToken) return;

    const payload = {
      name: values.name,
      companyType: values.companyType as CrmCompanyDto["companyType"],
      website: values.website || null,
      domain: values.domain || null,
      industry: values.industry || null,
      country: values.country || null,
      city: values.city || null,
      address: values.address || null,
      phone: values.phone || null,
      email: values.email || null,
      status: values.status as CrmCompanyDto["status"],
      priority: values.priority as CrmCompanyDto["priority"],
      assignedOwnerId: values.assignedOwnerId || null,
      leadSource: values.leadSource || null,
      notes: values.notes || null,
    };

    if (modalState?.company) {
      await updateCrmCompany(modalState.company.id, payload, accessToken);
      showToast("Company updated successfully.");
    } else {
      await createCrmCompany(payload, accessToken);
      showToast("Company created successfully.");
    }

    setModalState(null);
    await loadCompanies();
  };

  const handleToggleArchive = async (company: CrmCompanyDto) => {
    if (!accessToken) return;

    if (!company.isArchived) {
      setArchiveTarget(company);
      return;
    }

    try {
      await unarchiveCrmCompany(company.id, accessToken);
      showToast("Company restored.");
      await loadCompanies();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the company.");
    }
  };

  const handleConfirmArchive = async () => {
    if (!accessToken || !archiveTarget) return;

    setIsArchiving(true);
    try {
      await archiveCrmCompany(archiveTarget.id, accessToken);
      showToast("Company archived.");
      await loadCompanies();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the company.");
    } finally {
      setIsArchiving(false);
      setArchiveTarget(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <PageHeader
          title="CRM — Companies"
          description="Hotels, travel agencies, corporate clients, and other business partners."
          actions={
            <button type="button" onClick={() => setModalState({})}>
              + New Company
            </button>
          }
        />
        <CrmNav />

        <div className="row" style={{ marginBottom: "1rem" }}>
          <input
            type="search"
            placeholder="Search companies..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            aria-label="Search companies"
          />

          <label>
            Type:{" "}
            <select
              value={typeFilter}
              onChange={(e) => {
                setPage(1);
                setTypeFilter(e.target.value);
              }}
            >
              <option value="">All</option>
              {CRM_COMPANY_TYPES.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </label>

          <label>
            Status:{" "}
            <select
              value={statusFilter}
              onChange={(e) => {
                setPage(1);
                setStatusFilter(e.target.value);
              }}
            >
              <option value="">All</option>
              {CRM_LIFECYCLE_STATUSES.map((status) => (
                <option key={status} value={status}>
                  {status}
                </option>
              ))}
            </select>
          </label>
        </div>

        {error && <p role="alert">{error}</p>}

        {isLoading ? (
          <div className="stack">
            <div className="skeleton skeleton-line" style={{ height: 40 }} />
            <div className="skeleton skeleton-line" style={{ height: 40 }} />
          </div>
        ) : companies.length === 0 ? (
          <div className="empty-state">
            <p className="empty-state__title">No Companies Found</p>
            <p>Try adjusting your filters, or add the first company.</p>
          </div>
        ) : (
          <div className="table-responsive" style={{ overflowX: "auto" }}>
            <table>
              <thead>
                <tr>
                  <th>Company</th>
                  <th>Type</th>
                  <th>City / Country</th>
                  <th>Owner</th>
                  <th>Status</th>
                  <th>Priority</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {companies.map((company) => (
                  <tr key={company.id}>
                    <td data-label="Company">{company.name}</td>
                    <td data-label="Type">{company.companyType}</td>
                    <td data-label="City / Country">{[company.city, company.country].filter(Boolean).join(", ") || "—"}</td>
                    <td data-label="Owner">{company.assignedOwner ? `${company.assignedOwner.firstName} ${company.assignedOwner.lastName}` : "Unassigned"}</td>
                    <td data-label="Status">
                      <StatusBadge status={company.status} />
                    </td>
                    <td data-label="Priority">{company.priority}</td>
                    <td data-label="Actions">
                      <div className="row" style={{ gap: "var(--space-2)" }}>
                        <Link to={`/admin/crm/companies/${company.id}`}>View Details</Link>
                        <button type="button" className="btn-ghost" onClick={() => setModalState({ company })}>
                          Edit
                        </button>
                        <button type="button" className="btn-ghost" onClick={() => handleToggleArchive(company)}>
                          {company.isArchived ? "Restore" : "Archive"}
                        </button>
                        <button type="button" className="btn-ghost" onClick={() => setComposeTarget(company)}>
                          Send Email
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {totalPages > 1 && (
          <div className="row" style={{ marginTop: "1rem" }}>
            <button type="button" className="btn-secondary" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              Previous
            </button>
            <span>
              Page {page} of {totalPages}
            </span>
            <button type="button" className="btn-secondary" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
              Next
            </button>
          </div>
        )}

        {modalState && (
          <CrmCompanyFormModal
            company={modalState.company}
            administrators={administrators}
            onSave={handleSave}
            onClose={() => setModalState(null)}
          />
        )}

        {archiveTarget && (
          <ConfirmDialog
            title="Archive Company"
            message={`Archive ${archiveTarget.name}? It will be hidden from the default list, but its deals, contacts, and activity history remain intact.`}
            confirmLabel="Archive"
            danger
            isBusy={isArchiving}
            onConfirm={handleConfirmArchive}
            onCancel={() => setArchiveTarget(null)}
          />
        )}

        {composeTarget && (
          <ComposeEmailModal
            prefill={{
              toAddresses: composeTarget.email ? [{ address: composeTarget.email, name: composeTarget.name }] : [],
              companyId: composeTarget.id,
            }}
            onClose={() => setComposeTarget(null)}
            onSent={() => {
              setComposeTarget(null);
              showToast("Email sent.");
            }}
          />
        )}
      </main>
    </div>
  );
}

export default CrmCompaniesPage;
