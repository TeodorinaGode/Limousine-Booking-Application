import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../../../../context/AuthContext";
import { useToast } from "../../../../context/ToastContext";
import { getAdministrators } from "../../../../services/administratorService";
import { createCrmActivity } from "../../../../services/crmActivityService";
import {
  getCrmCompanyById,
  linkCrmCompanyContact,
  recordCrmCompanyContacted,
  scheduleCrmCompanyFollowUp,
  unlinkCrmCompanyContact,
} from "../../../../services/crmCompanyService";
import { getCrmContacts } from "../../../../services/crmContactService";
import { createCrmTask } from "../../../../services/crmTaskService";
import AdminNav from "../../../../components/AdminNav";
import ComposeEmailModal from "../../../../components/ComposeEmailModal";
import CrmNav from "../../../../components/CrmNav";
import LogActivityModal from "../../../../components/LogActivityModal";
import StatusBadge from "../../../../components/StatusBadge";
import TaskFormModal, { type TaskFormValues } from "../../../../components/TaskFormModal";
import type { AdministratorSummaryDto } from "../../../../types/administrator";
import type { CrmCompanyDetailDto } from "../../../../types/crmCompany";
import type { CrmContactDto } from "../../../../types/crmContact";

function formatDateTime(iso: string | null): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleString(undefined, { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function CrmCompanyDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { accessToken } = useAuth();
  const { showToast } = useToast();

  const [detail, setDetail] = useState<CrmCompanyDetailDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [followUpInput, setFollowUpInput] = useState("");
  const [linkCandidates, setLinkCandidates] = useState<CrmContactDto[]>([]);
  const [linkContactId, setLinkContactId] = useState("");
  const [isPrimaryLink, setIsPrimaryLink] = useState(false);
  const [isLinking, setIsLinking] = useState(false);
  const [administrators, setAdministrators] = useState<AdministratorSummaryDto[]>([]);
  const [isLogActivityOpen, setIsLogActivityOpen] = useState(false);
  const [isTaskFormOpen, setIsTaskFormOpen] = useState(false);
  const [isComposeOpen, setIsComposeOpen] = useState(false);

  const loadDetail = useCallback(async () => {
    if (!accessToken || !id) return;
    try {
      const result = await getCrmCompanyById(id, accessToken);
      setDetail(result);
      setFollowUpInput(result.company.nextFollowUpAt ? result.company.nextFollowUpAt.slice(0, 16) : "");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the company.");
    }
  }, [accessToken, id]);

  const loadLinkCandidates = useCallback(async () => {
    if (!accessToken) return;
    try {
      const result = await getCrmContacts({ pageSize: 100 }, accessToken);
      setLinkCandidates(result.items);
    } catch {
      // Non-fatal — the link picker just stays empty.
    }
  }, [accessToken]);

  useEffect(() => {
    loadDetail();
  }, [loadDetail]);

  useEffect(() => {
    loadLinkCandidates();
  }, [loadLinkCandidates]);

  useEffect(() => {
    if (!accessToken) return;
    getAdministrators(accessToken)
      .then(setAdministrators)
      .catch(() => {
        // Non-fatal — the task-assignee dropdown just stays empty.
      });
  }, [accessToken]);

  const handleRecordContacted = async () => {
    if (!accessToken || !id) return;
    try {
      await recordCrmCompanyContacted(id, new Date().toISOString(), accessToken);
      showToast("Logged as contacted today.");
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to record contact.");
    }
  };

  const handleSaveFollowUp = async () => {
    if (!accessToken || !id) return;
    try {
      await scheduleCrmCompanyFollowUp(id, followUpInput ? new Date(followUpInput).toISOString() : null, accessToken);
      showToast("Follow-up date updated.");
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the follow-up date.");
    }
  };

  const handleLinkContact = async () => {
    if (!accessToken || !id || !linkContactId) return;
    setIsLinking(true);
    try {
      await linkCrmCompanyContact(id, linkContactId, isPrimaryLink, accessToken);
      showToast("Contact linked.");
      setLinkContactId("");
      setIsPrimaryLink(false);
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to link the contact.");
    } finally {
      setIsLinking(false);
    }
  };

  const handleLogActivity = async (values: Parameters<typeof createCrmActivity>[0]) => {
    if (!accessToken) return;
    await createCrmActivity(values, accessToken);
    showToast("Activity logged.");
    setIsLogActivityOpen(false);
    await loadDetail();
  };

  const handleCreateTask = async (values: TaskFormValues) => {
    if (!accessToken || !id) return;
    await createCrmTask(
      {
        title: values.title,
        description: values.description || null,
        dueDate: values.dueDate,
        dueTime: values.dueTime || null,
        assignedUserId: values.assignedUserId,
        priority: values.priority as Parameters<typeof createCrmTask>[0]["priority"],
        companyId: id,
      },
      accessToken
    );
    showToast("Task created successfully.");
    setIsTaskFormOpen(false);
    await loadDetail();
  };

  const handleUnlinkContact = async (contactId: string) => {
    if (!accessToken || !id) return;
    try {
      await unlinkCrmCompanyContact(id, contactId, accessToken);
      showToast("Contact unlinked.");
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to unlink the contact.");
    }
  };

  if (error)
    return (
      <div className="app-shell">
        <AdminNav />
        <main className="app-main">
          <p role="alert">{error}</p>
        </main>
      </div>
    );
  if (!detail)
    return (
      <div className="app-shell">
        <AdminNav />
        <main className="app-main">
          <div className="skeleton skeleton-line" style={{ height: 40, maxWidth: 300 }} />
        </main>
      </div>
    );

  const { company, contacts, deals, openTasks, recentActivities } = detail;
  const linkedContactIds = new Set(contacts.map((c) => c.id));
  const availableToLink = linkCandidates.filter((c) => !linkedContactIds.has(c.id));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <CrmNav />
        <nav aria-label="Breadcrumb" className="crm-breadcrumb">
          <Link to="/admin/crm/companies">&larr; Companies</Link>
          <span className="crm-breadcrumb__sep" aria-hidden="true">/</span>
          <span className="crm-breadcrumb__current">{company.name}</span>
        </nav>
        <h1>{company.name}</h1>
        <div className="row" style={{ marginBottom: "var(--space-4)" }}>
          <StatusBadge status={company.status} />
          <span className="badge">{company.companyType}</span>
          <span className="badge">{company.priority} priority</span>
          {company.isArchived && <span className="badge badge--cancelled">Archived</span>}
        </div>
        <div className="row" style={{ marginBottom: "var(--space-4)" }}>
          <button type="button" className="btn-secondary" onClick={() => setIsLogActivityOpen(true)}>
            Log Activity
          </button>
          <button type="button" className="btn-secondary" onClick={() => setIsTaskFormOpen(true)}>
            + Task
          </button>
          <button type="button" className="btn-secondary" onClick={() => setIsComposeOpen(true)}>
            Compose Email
          </button>
        </div>

        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Company Information</h2>
          <p>Location: {[company.city, company.country].filter(Boolean).join(", ") || "—"}</p>
          <p>Address: {company.address ?? "—"}</p>
          <p>Phone: {company.phone ?? "—"}</p>
          <p>Email: {company.email ?? "—"}</p>
          <p>Website: {company.website ?? "—"}</p>
          <p>Industry: {company.industry ?? "—"}</p>
          <p>Lead source: {company.leadSource ?? "—"}</p>
          <p>Owner: {company.assignedOwner ? `${company.assignedOwner.firstName} ${company.assignedOwner.lastName}` : "Unassigned"}</p>
          {company.notes && <p>Notes: {company.notes}</p>}
        </section>

        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Follow-up</h2>
          <p>Last contacted: {formatDateTime(company.lastContactedAt)}</p>
          <div className="row" style={{ marginBottom: "var(--space-2)" }}>
            <button type="button" className="btn-secondary" onClick={handleRecordContacted}>
              Log Contacted Today
            </button>
          </div>
          <label>
            Next follow-up:{" "}
            <input type="datetime-local" value={followUpInput} onChange={(e) => setFollowUpInput(e.target.value)} />
          </label>{" "}
          <button type="button" className="btn-secondary" onClick={handleSaveFollowUp}>
            Save
          </button>
        </section>

        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Linked Contacts</h2>
          {contacts.length === 0 ? (
            <p>No contacts linked yet.</p>
          ) : (
            <ul>
              {contacts.map((contact) => (
                <li key={contact.id}>
                  <Link to={`/admin/crm/contacts/${contact.id}`}>
                    {contact.firstName} {contact.lastName}
                  </Link>
                  {contact.jobTitle && ` — ${contact.jobTitle}`}
                  {contact.isPrimary && " (Primary)"}{" "}
                  <button type="button" className="btn-ghost" onClick={() => handleUnlinkContact(contact.id)}>
                    Unlink
                  </button>
                </li>
              ))}
            </ul>
          )}

          <div className="row" style={{ marginTop: "var(--space-3)" }}>
            <select value={linkContactId} onChange={(e) => setLinkContactId(e.target.value)} aria-label="Contact to link">
              <option value="">Select a contact to link...</option>
              {availableToLink.map((contact) => (
                <option key={contact.id} value={contact.id}>
                  {contact.firstName} {contact.lastName} ({contact.email})
                </option>
              ))}
            </select>
            <label>
              <input type="checkbox" checked={isPrimaryLink} onChange={(e) => setIsPrimaryLink(e.target.checked)} /> Primary contact
            </label>
            <button type="button" className="btn-secondary" disabled={!linkContactId || isLinking} onClick={handleLinkContact}>
              {isLinking ? "Linking..." : "Link Contact"}
            </button>
          </div>
        </section>

        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Deals</h2>
          {deals.length === 0 ? (
            <p>No deals yet for this company.</p>
          ) : (
            <div className="table-responsive" style={{ overflowX: "auto" }}>
              <table>
                <thead>
                  <tr>
                    <th>Deal</th>
                    <th>Stage</th>
                    <th>Value</th>
                    <th>Probability</th>
                  </tr>
                </thead>
                <tbody>
                  {deals.map((deal) => (
                    <tr key={deal.id}>
                      <td data-label="Deal">{deal.name}</td>
                      <td data-label="Stage">
                        <StatusBadge status={deal.isWon ? "Completed" : deal.isLost ? "Cancelled" : deal.stageName} />
                      </td>
                      <td data-label="Value">
                        {deal.value.toLocaleString()} {deal.currency}
                      </td>
                      <td data-label="Probability">{deal.probability}%</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Open Tasks</h2>
          {openTasks.length === 0 ? (
            <p>No open tasks.</p>
          ) : (
            <ul>
              {openTasks.map((task) => (
                <li key={task.id}>
                  {task.title} — due {task.dueDate} ({task.priority})
                </li>
              ))}
            </ul>
          )}
        </section>

        <section>
          <h2>Recent Activity</h2>
          {recentActivities.length === 0 ? (
            <p>No activity logged yet.</p>
          ) : (
            <ul>
              {recentActivities.map((activity) => (
                <li key={activity.id}>
                  <strong>{(activity.activityType === "EmailSent" || activity.activityType === "EmailReceived") && "✉️ "}{activity.activityType}</strong> — {activity.subject} ({formatDateTime(activity.occurredAt)}, logged by{" "}
                  {activity.loggedBy.firstName} {activity.loggedBy.lastName})
                  {activity.emailThreadId && (
                    <>
                      {" — "}
                      <Link to={`/admin/crm/inbox?thread=${activity.emailThreadId}`}>View conversation</Link>
                    </>
                  )}
                </li>
              ))}
            </ul>
          )}
        </section>

        {isLogActivityOpen && (
          <LogActivityModal companyId={id} onSave={handleLogActivity} onClose={() => setIsLogActivityOpen(false)} />
        )}

        {isTaskFormOpen && (
          <TaskFormModal administrators={administrators} onSave={handleCreateTask} onClose={() => setIsTaskFormOpen(false)} />
        )}

        {isComposeOpen && (
          <ComposeEmailModal
            prefill={{
              toAddresses: company.email ? [{ address: company.email, name: company.name }] : [],
              companyId: company.id,
            }}
            onClose={() => setIsComposeOpen(false)}
            onSent={() => {
              setIsComposeOpen(false);
              loadDetail();
            }}
          />
        )}
      </main>
    </div>
  );
}

export default CrmCompanyDetailsPage;
