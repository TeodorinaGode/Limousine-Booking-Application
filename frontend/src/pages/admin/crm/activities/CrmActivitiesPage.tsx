import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../../../context/AuthContext";
import { useToast } from "../../../../context/ToastContext";
import { createCrmActivity, getCrmActivities } from "../../../../services/crmActivityService";
import AdminNav from "../../../../components/AdminNav";
import CrmNav from "../../../../components/CrmNav";
import LogActivityModal from "../../../../components/LogActivityModal";
import PageHeader from "../../../../components/PageHeader";
import { CRM_ACTIVITY_TYPES, type CrmActivityDto } from "../../../../types/crmActivity";

const PAGE_SIZE = 25;

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString(undefined, { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function CrmActivitiesPage() {
  const { accessToken } = useAuth();
  const { showToast } = useToast();

  const [activities, setActivities] = useState<CrmActivityDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [typeFilter, setTypeFilter] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isLogModalOpen, setIsLogModalOpen] = useState(false);

  const loadActivities = useCallback(async () => {
    if (!accessToken) return;
    setIsLoading(true);
    setError(null);
    try {
      const result = await getCrmActivities(
        {
          activityType: typeFilter || undefined,
          from: fromDate ? new Date(fromDate).toISOString() : undefined,
          to: toDate ? new Date(toDate).toISOString() : undefined,
          page,
          pageSize: PAGE_SIZE,
        },
        accessToken
      );
      setActivities(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load activities.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, typeFilter, fromDate, toDate, page]);

  useEffect(() => {
    loadActivities();
  }, [loadActivities]);

  const handleLogActivity = async (values: Parameters<typeof createCrmActivity>[0]) => {
    if (!accessToken) return;
    await createCrmActivity(values, accessToken);
    showToast("Activity logged.");
    setIsLogModalOpen(false);
    setPage(1);
    await loadActivities();
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <PageHeader
          title="CRM — Activities"
          description="A chronological log of calls, emails, meetings, and notes across every company, contact, and deal."
          actions={
            <button type="button" onClick={() => setIsLogModalOpen(true)}>
              + Log Activity
            </button>
          }
        />
        <CrmNav />

        <div className="row" style={{ marginBottom: "1rem" }}>
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
              {CRM_ACTIVITY_TYPES.map((type) => (
                <option key={type} value={type}>
                  {type}
                </option>
              ))}
            </select>
          </label>
          <label>
            From:{" "}
            <input
              type="date"
              value={fromDate}
              onChange={(e) => {
                setPage(1);
                setFromDate(e.target.value);
              }}
            />
          </label>
          <label>
            To:{" "}
            <input
              type="date"
              value={toDate}
              onChange={(e) => {
                setPage(1);
                setToDate(e.target.value);
              }}
            />
          </label>
        </div>

        {error && <p role="alert">{error}</p>}

        {isLoading ? (
          <div className="stack">
            <div className="skeleton skeleton-line" style={{ height: 40 }} />
            <div className="skeleton skeleton-line" style={{ height: 40 }} />
          </div>
        ) : activities.length === 0 ? (
          <div className="empty-state">
            <p className="empty-state__title">No Activity Logged Yet</p>
          </div>
        ) : (
          <div className="table-responsive" style={{ overflowX: "auto" }}>
            <table>
              <thead>
                <tr>
                  <th>When</th>
                  <th>Type</th>
                  <th>Subject</th>
                  <th>Related to</th>
                  <th>Logged by</th>
                </tr>
              </thead>
              <tbody>
                {activities.map((activity) => (
                  <tr key={activity.id}>
                    <td data-label="When">{formatDateTime(activity.occurredAt)}</td>
                    <td data-label="Type">{(activity.activityType === "EmailSent" || activity.activityType === "EmailReceived") && "✉️ "}{activity.activityType}</td>
                    <td data-label="Subject">
                      {activity.subject}
                      {activity.emailThreadId && (
                        <>
                          {" "}
                          <Link to={`/admin/crm/inbox?thread=${activity.emailThreadId}`}>(View conversation)</Link>
                        </>
                      )}
                    </td>
                    <td data-label="Related to">
                      {activity.companyId && <Link to={`/admin/crm/companies/${activity.companyId}`}>{activity.companyName}</Link>}
                      {activity.contactId && (
                        <>
                          {activity.companyId ? " / " : ""}
                          <Link to={`/admin/crm/contacts/${activity.contactId}`}>{activity.contactName}</Link>
                        </>
                      )}
                      {activity.dealId && (
                        <>
                          {activity.companyId || activity.contactId ? " / " : ""}
                          <Link to={`/admin/crm/deals/${activity.dealId}`}>{activity.dealName}</Link>
                        </>
                      )}
                    </td>
                    <td data-label="Logged by">
                      {activity.loggedBy.firstName} {activity.loggedBy.lastName}
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

        {isLogModalOpen && <LogActivityModal onSave={handleLogActivity} onClose={() => setIsLogModalOpen(false)} />}
      </main>
    </div>
  );
}

export default CrmActivitiesPage;
