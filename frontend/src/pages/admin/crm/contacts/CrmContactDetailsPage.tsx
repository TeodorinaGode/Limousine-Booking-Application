import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../../../../context/AuthContext";
import { useToast } from "../../../../context/ToastContext";
import { getAdministrators } from "../../../../services/administratorService";
import { createCrmActivity } from "../../../../services/crmActivityService";
import {
  getCrmCompanies,
  linkCrmCompanyContact,
  unlinkCrmCompanyContact,
} from "../../../../services/crmCompanyService";
import {
  getCrmContactById,
  recordCrmContactContacted,
  scheduleCrmContactFollowUp,
} from "../../../../services/crmContactService";
import { createCrmTask } from "../../../../services/crmTaskService";
import AdminNav from "../../../../components/AdminNav";
import ComposeEmailModal from "../../../../components/ComposeEmailModal";
import CrmNav from "../../../../components/CrmNav";
import LogActivityModal from "../../../../components/LogActivityModal";
import StatusBadge from "../../../../components/StatusBadge";
import TaskFormModal, { type TaskFormValues } from "../../../../components/TaskFormModal";
import type { AdministratorSummaryDto } from "../../../../types/administrator";
import type { CrmCompanyDto } from "../../../../types/crmCompany";
import type { CrmContactDetailDto } from "../../../../types/crmContact";

function formatDateTime(iso: string | null): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleString(undefined, { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function CrmContactDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { accessToken } = useAuth();
  const { showToast } = useToast();

  const [detail, setDetail] = useState<CrmContactDetailDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [followUpInput, setFollowUpInput] = useState("");
  const [linkCandidates, setLinkCandidates] = useState<CrmCompanyDto[]>([]);
  const [linkCompanyId, setLinkCompanyId] = useState("");
  const [isPrimaryLink, setIsPrimaryLink] = useState(false);
  const [isLinking, setIsLinking] = useState(false);
  const [administrators, setAdministrators] = useState<AdministratorSummaryDto[]>([]);
  const [isLogActivityOpen, setIsLogActivityOpen] = useState(false);
  const [isTaskFormOpen, setIsTaskFormOpen] = useState(false);
  const [isComposeOpen, setIsComposeOpen] = useState(false);

  const loadDetail = useCallback(async () => {
    if (!accessToken || !id) return;
    try {
      const result = await getCrmContactById(id, accessToken);
      setDetail(result);
      setFollowUpInput(result.contact.nextFollowUpAt ? result.contact.nextFollowUpAt.slice(0, 16) : "");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the contact.");
    }
  }, [accessToken, id]);

  const loadLinkCandidates = useCallback(async () => {
    if (!accessToken) return;
    try {
      const result = await getCrmCompanies({ pageSize: 100 }, accessToken);
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
        contactId: id,
      },
      accessToken
    );
    showToast("Task created successfully.");
    setIsTaskFormOpen(false);
    await loadDetail();
  };

  const handleRecordContacted = async () => {
    if (!accessToken || !id) return;
    try {
      await recordCrmContactContacted(id, new Date().toISOString(), accessToken);
      showToast("Logged as contacted today.");
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to record contact.");
    }
  };

  const handleSaveFollowUp = async () => {
    if (!accessToken || !id) return;
    try {
      await scheduleCrmContactFollowUp(id, followUpInput ? new Date(followUpInput).toISOString() : null, accessToken);
      showToast("Follow-up date updated.");
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the follow-up date.");
    }
  };

  const handleLinkCompany = async () => {
    if (!accessToken || !id || !linkCompanyId) return;
    setIsLinking(true);
    try {
      await linkCrmCompanyContact(linkCompanyId, id, isPrimaryLink, accessToken);
      showToast("Company linked.");
      setLinkCompanyId("");
      setIsPrimaryLink(false);
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to link the company.");
    } finally {
      setIsLinking(false);
    }
  };

  const handleUnlinkCompany = async (companyId: string) => {
    if (!accessToken || !id) return;
    try {
      await unlinkCrmCompanyContact(companyId, id, accessToken);
      showToast("Company unlinked.");
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to unlink the company.");
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

  const { contact, companies, deals, openTasks, recentActivities } = detail;
  const linkedCompanyIds = new Set(companies.map((c) => c.id));
  const availableToLink = linkCandidates.filter((c) => !linkedCompanyIds.has(c.id));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <CrmNav />
        <nav aria-label="Breadcrumb" className="crm-breadcrumb">
          <Link to="/admin/crm/contacts">&larr; Contacts</Link>
          <span className="crm-breadcrumb__sep" aria-hidden="true">/</span>
          <span className="crm-breadcrumb__current">{contact.firstName} {contact.lastName}</span>
        </nav>
        <h1>
          {contact.firstName} {contact.lastName}
        </h1>
        <div className="row" style={{ marginBottom: "var(--space-4)" }}>
          <StatusBadge status={contact.status} />
          {contact.jobTitle && <span className="badge">{contact.jobTitle}</span>}
          {contact.isArchived && <span className="badge badge--cancelled">Archived</span>}
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
          <h2>Contact Information</h2>
          <p>Email: {contact.email}</p>
          <p>Phone: {contact.phone ?? "—"}</p>
          <p>Preferred contact method: {contact.preferredContactMethod ?? "No preference"}</p>
          <p>Owner: {contact.assignedOwner ? `${contact.assignedOwner.firstName} ${contact.assignedOwner.lastName}` : "Unassigned"}</p>
          {contact.notes && <p>Notes: {contact.notes}</p>}
        </section>

        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Follow-up</h2>
          <p>Last contacted: {formatDateTime(contact.lastContactedAt)}</p>
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
          <h2>Linked Companies</h2>
          {companies.length === 0 ? (
            <p>No companies linked yet.</p>
          ) : (
            <ul>
              {companies.map((company) => (
                <li key={company.id}>
                  <Link to={`/admin/crm/companies/${company.id}`}>{company.name}</Link> ({company.companyType}){" "}
                  <button type="button" className="btn-ghost" onClick={() => handleUnlinkCompany(company.id)}>
                    Unlink
                  </button>
                </li>
              ))}
            </ul>
          )}

          <div className="row" style={{ marginTop: "var(--space-3)" }}>
            <select value={linkCompanyId} onChange={(e) => setLinkCompanyId(e.target.value)} aria-label="Company to link">
              <option value="">Select a company to link...</option>
              {availableToLink.map((company) => (
                <option key={company.id} value={company.id}>
                  {company.name}
                </option>
              ))}
            </select>
            <label>
              <input type="checkbox" checked={isPrimaryLink} onChange={(e) => setIsPrimaryLink(e.target.checked)} /> Primary contact there
            </label>
            <button type="button" className="btn-secondary" disabled={!linkCompanyId || isLinking} onClick={handleLinkCompany}>
              {isLinking ? "Linking..." : "Link Company"}
            </button>
          </div>
        </section>

        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Deals</h2>
          {deals.length === 0 ? (
            <p>No deals associated with this contact.</p>
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
          <LogActivityModal contactId={id} onSave={handleLogActivity} onClose={() => setIsLogActivityOpen(false)} />
        )}

        {isTaskFormOpen && (
          <TaskFormModal administrators={administrators} onSave={handleCreateTask} onClose={() => setIsTaskFormOpen(false)} />
        )}

        {isComposeOpen && (
          <ComposeEmailModal
            prefill={{
              toAddresses: [{ address: contact.email, name: `${contact.firstName} ${contact.lastName}` }],
              contactId: contact.id,
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

export default CrmContactDetailsPage;
