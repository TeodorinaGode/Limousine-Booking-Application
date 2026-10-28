import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../../../context/AuthContext";
import { useToast } from "../../../../context/ToastContext";
import { getAdministrators } from "../../../../services/administratorService";
import {
  archiveCrmContact,
  createCrmContact,
  getCrmContacts,
  unarchiveCrmContact,
  updateCrmContact,
} from "../../../../services/crmContactService";
import AdminNav from "../../../../components/AdminNav";
import CrmNav from "../../../../components/CrmNav";
import ConfirmDialog from "../../../../components/ConfirmDialog";
import PageHeader from "../../../../components/PageHeader";
import StatusBadge from "../../../../components/StatusBadge";
import type { AdministratorSummaryDto } from "../../../../types/administrator";
import { CRM_LIFECYCLE_STATUSES } from "../../../../types/crmCompany";
import type { CrmContactDto } from "../../../../types/crmContact";
import CrmContactFormModal, { type CrmContactFormValues } from "./CrmContactFormModal";

const PAGE_SIZE = 20;

function CrmContactsPage() {
  const { accessToken } = useAuth();
  const { showToast } = useToast();

  const [contacts, setContacts] = useState<CrmContactDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [administrators, setAdministrators] = useState<AdministratorSummaryDto[]>([]);
  const [modalState, setModalState] = useState<{ contact?: CrmContactDto } | null>(null);
  const [archiveTarget, setArchiveTarget] = useState<CrmContactDto | null>(null);
  const [isArchiving, setIsArchiving] = useState(false);

  const loadContacts = useCallback(async () => {
    if (!accessToken) return;
    setIsLoading(true);
    setError(null);
    try {
      const result = await getCrmContacts({ search, status: statusFilter || undefined, page, pageSize: PAGE_SIZE }, accessToken);
      setContacts(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load contacts.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, search, statusFilter, page]);

  const loadAdministrators = useCallback(async () => {
    if (!accessToken) return;
    try {
      setAdministrators(await getAdministrators(accessToken));
    } catch {
      // Non-fatal — the owner dropdown just falls back to "Unassigned" only.
    }
  }, [accessToken]);

  useEffect(() => {
    loadContacts();
  }, [loadContacts]);

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

  const handleSave = async (values: CrmContactFormValues) => {
    if (!accessToken) return;

    const payload = {
      firstName: values.firstName,
      lastName: values.lastName,
      email: values.email,
      jobTitle: values.jobTitle || null,
      phone: values.phone || null,
      preferredContactMethod: values.preferredContactMethod || null,
      status: values.status as CrmContactDto["status"],
      assignedOwnerId: values.assignedOwnerId || null,
      notes: values.notes || null,
    };

    if (modalState?.contact) {
      await updateCrmContact(modalState.contact.id, payload, accessToken);
      showToast("Contact updated successfully.");
    } else {
      await createCrmContact(payload, accessToken);
      showToast("Contact created successfully.");
    }

    setModalState(null);
    await loadContacts();
  };

  const handleToggleArchive = async (contact: CrmContactDto) => {
    if (!accessToken) return;

    if (!contact.isArchived) {
      setArchiveTarget(contact);
      return;
    }

    try {
      await unarchiveCrmContact(contact.id, accessToken);
      showToast("Contact restored.");
      await loadContacts();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the contact.");
    }
  };

  const handleConfirmArchive = async () => {
    if (!accessToken || !archiveTarget) return;

    setIsArchiving(true);
    try {
      await archiveCrmContact(archiveTarget.id, accessToken);
      showToast("Contact archived.");
      await loadContacts();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the contact.");
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
          title="CRM — Contacts"
          description="People at partner companies — concierge managers, travel coordinators, event planners."
          actions={
            <button type="button" onClick={() => setModalState({})}>
              + New Contact
            </button>
          }
        />
        <CrmNav />

        <div className="row" style={{ marginBottom: "1rem" }}>
          <input
            type="search"
            placeholder="Search contacts..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            aria-label="Search contacts"
          />

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
        ) : contacts.length === 0 ? (
          <div className="empty-state">
            <p className="empty-state__title">No Contacts Found</p>
            <p>Try adjusting your filters, or add the first contact.</p>
          </div>
        ) : (
          <div className="table-responsive" style={{ overflowX: "auto" }}>
            <table>
              <thead>
                <tr>
                  <th>Contact</th>
                  <th>Job title</th>
                  <th>Email</th>
                  <th>Owner</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {contacts.map((contact) => (
                  <tr key={contact.id}>
                    <td data-label="Contact">
                      {contact.firstName} {contact.lastName}
                    </td>
                    <td data-label="Job title">{contact.jobTitle ?? "—"}</td>
                    <td data-label="Email">{contact.email}</td>
                    <td data-label="Owner">{contact.assignedOwner ? `${contact.assignedOwner.firstName} ${contact.assignedOwner.lastName}` : "Unassigned"}</td>
                    <td data-label="Status">
                      <StatusBadge status={contact.status} />
                    </td>
                    <td data-label="Actions">
                      <div className="row" style={{ gap: "var(--space-2)" }}>
                        <Link to={`/admin/crm/contacts/${contact.id}`}>View Details</Link>
                        <button type="button" className="btn-ghost" onClick={() => setModalState({ contact })}>
                          Edit
                        </button>
                        <button type="button" className="btn-ghost" onClick={() => handleToggleArchive(contact)}>
                          {contact.isArchived ? "Restore" : "Archive"}
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
          <CrmContactFormModal
            contact={modalState.contact}
            administrators={administrators}
            onSave={handleSave}
            onClose={() => setModalState(null)}
          />
        )}

        {archiveTarget && (
          <ConfirmDialog
            title="Archive Contact"
            message={`Archive ${archiveTarget.firstName} ${archiveTarget.lastName}? It will be hidden from the default list, but linked deals and activity history remain intact.`}
            confirmLabel="Archive"
            danger
            isBusy={isArchiving}
            onConfirm={handleConfirmArchive}
            onCancel={() => setArchiveTarget(null)}
          />
        )}
      </main>
    </div>
  );
}

export default CrmContactsPage;
