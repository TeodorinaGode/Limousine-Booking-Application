import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../../../context/AuthContext";
import { useToast } from "../../../../context/ToastContext";
import { getAdministrators } from "../../../../services/administratorService";
import { cancelCrmTask, completeCrmTask, createCrmTask, getCrmTasks } from "../../../../services/crmTaskService";
import AdminNav from "../../../../components/AdminNav";
import CrmNav from "../../../../components/CrmNav";
import PageHeader from "../../../../components/PageHeader";
import StatusBadge from "../../../../components/StatusBadge";
import TaskFormModal, { type TaskFormValues } from "../../../../components/TaskFormModal";
import type { AdministratorSummaryDto } from "../../../../types/administrator";
import type { CrmTaskDto } from "../../../../types/crmTask";

const PAGE_SIZE = 25;

type TaskTab = "all" | "mine" | "today" | "overdue" | "completed";

const TABS: { id: TaskTab; label: string }[] = [
  { id: "all", label: "All" },
  { id: "mine", label: "My Tasks" },
  { id: "today", label: "Due Today" },
  { id: "overdue", label: "Overdue" },
  { id: "completed", label: "Completed" },
];

function CrmTasksPage() {
  const { accessToken, user } = useAuth();
  const { showToast } = useToast();

  const [tab, setTab] = useState<TaskTab>("all");
  const [tasks, setTasks] = useState<CrmTaskDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [administrators, setAdministrators] = useState<AdministratorSummaryDto[]>([]);
  const [isFormOpen, setIsFormOpen] = useState(false);

  const loadTasks = useCallback(async () => {
    if (!accessToken) return;
    setIsLoading(true);
    setError(null);
    try {
      const result = await getCrmTasks(
        {
          assignedUserId: tab === "mine" ? user?.id : undefined,
          dueToday: tab === "today" ? true : undefined,
          overdue: tab === "overdue" ? true : undefined,
          status: tab === "completed" ? "Completed" : undefined,
          page,
          pageSize: PAGE_SIZE,
        },
        accessToken
      );
      setTasks(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load tasks.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, tab, user?.id, page]);

  const loadAdministrators = useCallback(async () => {
    if (!accessToken) return;
    try {
      setAdministrators(await getAdministrators(accessToken));
    } catch {
      // Non-fatal.
    }
  }, [accessToken]);

  useEffect(() => {
    loadTasks();
  }, [loadTasks]);

  useEffect(() => {
    loadAdministrators();
  }, [loadAdministrators]);

  const handleCreate = async (values: TaskFormValues) => {
    if (!accessToken) return;
    await createCrmTask(
      {
        title: values.title,
        description: values.description || null,
        dueDate: values.dueDate,
        dueTime: values.dueTime || null,
        assignedUserId: values.assignedUserId,
        priority: values.priority as CrmTaskDto["priority"],
      },
      accessToken
    );
    showToast("Task created successfully.");
    setIsFormOpen(false);
    await loadTasks();
  };

  const handleComplete = async (task: CrmTaskDto) => {
    if (!accessToken) return;
    try {
      await completeCrmTask(task.id, accessToken);
      showToast("Task completed.");
      await loadTasks();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the task.");
    }
  };

  const handleCancel = async (task: CrmTaskDto) => {
    if (!accessToken) return;
    try {
      await cancelCrmTask(task.id, accessToken);
      showToast("Task cancelled.");
      await loadTasks();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the task.");
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <PageHeader
          title="CRM — Tasks"
          description="Follow-ups and reminders, optionally linked to a company, contact, or deal."
          actions={
            <button type="button" onClick={() => setIsFormOpen(true)}>
              + New Task
            </button>
          }
        />
        <CrmNav />

        <div className="row" style={{ marginBottom: "1rem" }}>
          {TABS.map((t) => (
            <button
              key={t.id}
              type="button"
              className={tab === t.id ? "btn-secondary" : "btn-ghost"}
              onClick={() => {
                setPage(1);
                setTab(t.id);
              }}
            >
              {t.label}
            </button>
          ))}
        </div>

        {error && <p role="alert">{error}</p>}

        {isLoading ? (
          <div className="stack">
            <div className="skeleton skeleton-line" style={{ height: 40 }} />
            <div className="skeleton skeleton-line" style={{ height: 40 }} />
          </div>
        ) : tasks.length === 0 ? (
          <div className="empty-state">
            <p className="empty-state__title">No Tasks Found</p>
          </div>
        ) : (
          <div className="table-responsive" style={{ overflowX: "auto" }}>
            <table>
              <thead>
                <tr>
                  <th>Task</th>
                  <th>Due</th>
                  <th>Priority</th>
                  <th>Assigned to</th>
                  <th>Related to</th>
                  <th>Status</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {tasks.map((task) => (
                  <tr key={task.id}>
                    <td data-label="Task">{task.title}</td>
                    <td data-label="Due">
                      {task.dueDate}
                      {task.dueTime ? ` ${task.dueTime.slice(0, 5)}` : ""}
                    </td>
                    <td data-label="Priority">{task.priority}</td>
                    <td data-label="Assigned to">
                      {task.assignedUser.firstName} {task.assignedUser.lastName}
                    </td>
                    <td data-label="Related to">
                      {task.companyId && <Link to={`/admin/crm/companies/${task.companyId}`}>{task.companyName}</Link>}
                      {task.dealId && (
                        <>
                          {task.companyId ? " / " : ""}
                          <Link to={`/admin/crm/deals/${task.dealId}`}>{task.dealName}</Link>
                        </>
                      )}
                      {task.contactId && (
                        <>
                          {task.companyId || task.dealId ? " / " : ""}
                          <Link to={`/admin/crm/contacts/${task.contactId}`}>{task.contactName}</Link>
                        </>
                      )}
                      {!task.companyId && !task.dealId && !task.contactId && "—"}
                    </td>
                    <td data-label="Status">
                      <StatusBadge status={task.status} />
                    </td>
                    <td data-label="Actions">
                      {(task.status === "ToDo" || task.status === "InProgress") && (
                        <div className="row" style={{ gap: "var(--space-2)" }}>
                          <button type="button" className="btn-ghost" onClick={() => handleComplete(task)}>
                            Complete
                          </button>
                          <button type="button" className="btn-ghost" onClick={() => handleCancel(task)}>
                            Cancel
                          </button>
                        </div>
                      )}
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

        {isFormOpen && <TaskFormModal administrators={administrators} onSave={handleCreate} onClose={() => setIsFormOpen(false)} />}
      </main>
    </div>
  );
}

export default CrmTasksPage;
