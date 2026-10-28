import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../../../../context/AuthContext";
import { useToast } from "../../../../context/ToastContext";
import { getAdministrators } from "../../../../services/administratorService";
import { createCrmActivity } from "../../../../services/crmActivityService";
import { getCrmContacts } from "../../../../services/crmContactService";
import {
  getCrmDealBoard,
  getCrmDealById,
  linkCrmDealContact,
  moveCrmDealStage,
  unlinkCrmDealContact,
  updateCrmDeal,
} from "../../../../services/crmDealService";
import { createCrmTask } from "../../../../services/crmTaskService";
import AdminNav from "../../../../components/AdminNav";
import ComposeEmailModal from "../../../../components/ComposeEmailModal";
import CrmNav from "../../../../components/CrmNav";
import ConfirmDialog from "../../../../components/ConfirmDialog";
import LogActivityModal from "../../../../components/LogActivityModal";
import StatusBadge from "../../../../components/StatusBadge";
import TaskFormModal, { type TaskFormValues } from "../../../../components/TaskFormModal";
import type { AdministratorSummaryDto } from "../../../../types/administrator";
import type { CrmContactDto } from "../../../../types/crmContact";
import type { CrmDealDetailDto, CrmPipelineStageSummaryDto } from "../../../../types/crmDeal";
import CrmDealFormModal, { type CrmDealFormValues } from "./CrmDealFormModal";

function formatDateTime(iso: string | null): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleString(undefined, { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function CrmDealDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { accessToken } = useAuth();
  const { showToast } = useToast();

  const [detail, setDetail] = useState<CrmDealDetailDto | null>(null);
  const [stages, setStages] = useState<CrmPipelineStageSummaryDto[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isEditing, setIsEditing] = useState(false);
  const [linkCandidates, setLinkCandidates] = useState<CrmContactDto[]>([]);
  const [linkContactId, setLinkContactId] = useState("");
  const [isLinking, setIsLinking] = useState(false);
  const [pendingLostStageId, setPendingLostStageId] = useState<string | null>(null);
  const [administrators, setAdministrators] = useState<AdministratorSummaryDto[]>([]);
  const [isLogActivityOpen, setIsLogActivityOpen] = useState(false);
  const [isTaskFormOpen, setIsTaskFormOpen] = useState(false);
  const [isComposeOpen, setIsComposeOpen] = useState(false);

  const loadDetail = useCallback(async () => {
    if (!accessToken || !id) return;
    try {
      setDetail(await getCrmDealById(id, accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the deal.");
    }
  }, [accessToken, id]);

  const loadStages = useCallback(async () => {
    if (!accessToken) return;
    try {
      const board = await getCrmDealBoard(accessToken);
      setStages(board.columns.map((c) => c.stage).sort((a, b) => a.sortOrder - b.sortOrder));
    } catch {
      // Non-fatal — the manual stage selector just stays empty.
    }
  }, [accessToken]);

  const loadLinkCandidates = useCallback(async () => {
    if (!accessToken) return;
    try {
      const result = await getCrmContacts({ pageSize: 100 }, accessToken);
      setLinkCandidates(result.items);
    } catch {
      // Non-fatal.
    }
  }, [accessToken]);

  useEffect(() => {
    loadDetail();
  }, [loadDetail]);

  useEffect(() => {
    loadStages();
  }, [loadStages]);

  useEffect(() => {
    loadLinkCandidates();
  }, [loadLinkCandidates]);

  useEffect(() => {
    if (!accessToken) return;
    getAdministrators(accessToken)
      .then(setAdministrators)
      .catch(() => {
        // Non-fatal — the owner dropdown in the edit modal just stays empty.
      });
  }, [accessToken]);

  const handleSave = async (values: CrmDealFormValues) => {
    if (!accessToken || !id) return;
    await updateCrmDeal(
      id,
      {
        name: values.name,
        description: values.description || null,
        dealType: values.dealType || null,
        value: Number(values.value),
        currency: values.currency,
        expectedCloseDate: values.expectedCloseDate || null,
        ownerId: values.ownerId || null,
        nextAction: values.nextAction || null,
        notes: values.notes || null,
      },
      accessToken
    );
    showToast("Deal updated successfully.");
    setIsEditing(false);
    await loadDetail();
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
        dealId: id,
      },
      accessToken
    );
    showToast("Task created successfully.");
    setIsTaskFormOpen(false);
    await loadDetail();
  };

  const handleStageSelect = async (stageId: string) => {
    if (!accessToken || !id || !detail) return;
    const stage = stages.find((s) => s.id === stageId);
    if (!stage) return;

    if (stage.isLost) {
      setPendingLostStageId(stageId);
      return;
    }

    try {
      await moveCrmDealStage(id, { stageId }, accessToken);
      showToast("Stage updated.");
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to move the deal.");
    }
  };

  const handleConfirmLostStage = async (reason?: string) => {
    if (!accessToken || !id || !pendingLostStageId || !reason) {
      setPendingLostStageId(null);
      return;
    }

    try {
      await moveCrmDealStage(id, { stageId: pendingLostStageId, lossReason: reason }, accessToken);
      showToast("Deal marked as lost.");
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to move the deal.");
    } finally {
      setPendingLostStageId(null);
    }
  };

  const handleLinkContact = async () => {
    if (!accessToken || !id || !linkContactId) return;
    setIsLinking(true);
    try {
      await linkCrmDealContact(id, linkContactId, accessToken);
      showToast("Contact linked.");
      setLinkContactId("");
      await loadDetail();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to link the contact.");
    } finally {
      setIsLinking(false);
    }
  };

  const handleUnlinkContact = async (contactId: string) => {
    if (!accessToken || !id) return;
    try {
      await unlinkCrmDealContact(id, contactId, accessToken);
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

  const { deal, contacts, openTasks, recentActivities } = detail;
  const linkedContactIds = new Set(contacts.map((c) => c.id));
  const availableToLink = linkCandidates.filter((c) => !linkedContactIds.has(c.id));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <CrmNav />
        <nav aria-label="Breadcrumb" className="crm-breadcrumb">
          <Link to="/admin/crm/deals">&larr; Deals</Link>
          <span className="crm-breadcrumb__sep" aria-hidden="true">/</span>
          <span className="crm-breadcrumb__current">{deal.name}</span>
        </nav>
        <div className="row" style={{ justifyContent: "space-between" }}>
          <h1>{deal.name}</h1>
          <button type="button" className="btn-secondary" onClick={() => setIsEditing(true)}>
            Edit
          </button>
        </div>
        <div className="row" style={{ marginBottom: "var(--space-4)" }}>
          <StatusBadge status={deal.isWon ? "Completed" : deal.isLost ? "Cancelled" : deal.stageName} />
          <span className="badge">{deal.probability}% probability</span>
          {deal.isArchived && <span className="badge badge--cancelled">Archived</span>}
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
          <h2>Deal Information</h2>
          <p>
            Company: <Link to={`/admin/crm/companies/${deal.company.id}`}>{deal.company.name}</Link>
          </p>
          <p>
            Value: {deal.value.toLocaleString()} {deal.currency}
          </p>
          <p>Type: {deal.dealType ?? "—"}</p>
          <p>Expected close date: {deal.expectedCloseDate ?? "—"}</p>
          <p>Owner: {deal.owner ? `${deal.owner.firstName} ${deal.owner.lastName}` : "Unassigned"}</p>
          <p>Next action: {deal.nextAction ?? "—"}</p>
          {deal.description && <p>Description: {deal.description}</p>}
          {deal.notes && <p>Notes: {deal.notes}</p>}
          {deal.isLost && <p>Loss reason: {deal.lossReason}</p>}
          {deal.closedAt && <p>Closed at: {formatDateTime(deal.closedAt)}</p>}
        </section>

        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Stage</h2>
          <label>
            Move to stage:{" "}
            <select value={deal.stageId} onChange={(e) => handleStageSelect(e.target.value)}>
              {stages.map((stage) => (
                <option key={stage.id} value={stage.id}>
                  {stage.name}
                </option>
              ))}
            </select>
          </label>
        </section>

        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Associated Contacts</h2>
          {contacts.length === 0 ? (
            <p>No contacts linked yet.</p>
          ) : (
            <ul>
              {contacts.map((contact) => (
                <li key={contact.id}>
                  <Link to={`/admin/crm/contacts/${contact.id}`}>
                    {contact.firstName} {contact.lastName}
                  </Link>
                  {contact.jobTitle && ` — ${contact.jobTitle}`}{" "}
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
            <button type="button" className="btn-secondary" disabled={!linkContactId || isLinking} onClick={handleLinkContact}>
              {isLinking ? "Linking..." : "Link Contact"}
            </button>
          </div>
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
                  <strong>{(activity.activityType === "EmailSent" || activity.activityType === "EmailReceived") && "✉️ "}{activity.activityType}</strong> — {activity.subject} ({formatDateTime(activity.occurredAt)})
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

        {isEditing && (
          <CrmDealFormModal deal={deal} companies={[]} administrators={administrators} onSave={handleSave} onClose={() => setIsEditing(false)} />
        )}

        {isLogActivityOpen && (
          <LogActivityModal dealId={id} onSave={handleLogActivity} onClose={() => setIsLogActivityOpen(false)} />
        )}

        {isTaskFormOpen && (
          <TaskFormModal administrators={administrators} onSave={handleCreateTask} onClose={() => setIsTaskFormOpen(false)} />
        )}

        {isComposeOpen && (
          <ComposeEmailModal
            prefill={{
              toAddresses: contacts.length > 0 ? [{ address: contacts[0].email, name: `${contacts[0].firstName} ${contacts[0].lastName}` }] : [],
              dealId: deal.id,
            }}
            onClose={() => setIsComposeOpen(false)}
            onSent={() => {
              setIsComposeOpen(false);
              loadDetail();
            }}
          />
        )}

        {pendingLostStageId && (
          <ConfirmDialog
            title="Mark Deal as Lost"
            message="Moving this deal to a Lost stage requires a reason."
            confirmLabel="Mark as Lost"
            reasonLabel="Loss reason (required)"
            danger
            onConfirm={handleConfirmLostStage}
            onCancel={() => setPendingLostStageId(null)}
          />
        )}
      </main>
    </div>
  );
}

export default CrmDealDetailsPage;
