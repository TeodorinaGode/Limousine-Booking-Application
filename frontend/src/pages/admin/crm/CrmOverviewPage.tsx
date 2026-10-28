import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../../context/AuthContext";
import { getCrmDashboard } from "../../../services/crmAnalyticsService";
import AdminNav from "../../../components/AdminNav";
import CrmNav from "../../../components/CrmNav";
import PageHeader from "../../../components/PageHeader";
import MetricCard from "../reports/MetricCard";
import type { CrmDashboardDto } from "../../../types/crmAnalytics";

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString(undefined, { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" });
}

function activityTargetLink(activity: CrmDashboardDto["recentActivities"][number]): { to: string; label: string } | null {
  if (activity.dealId) return { to: `/admin/crm/deals/${activity.dealId}`, label: activity.dealName ?? "Deal" };
  if (activity.companyId) return { to: `/admin/crm/companies/${activity.companyId}`, label: activity.companyName ?? "Company" };
  if (activity.contactId) return { to: `/admin/crm/contacts/${activity.contactId}`, label: activity.contactName ?? "Contact" };
  return null;
}

function CrmOverviewPage() {
  const { accessToken } = useAuth();

  const [dashboard, setDashboard] = useState<CrmDashboardDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      setIsLoading(true);
      setError(null);
      try {
        setDashboard(await getCrmDashboard(accessToken));
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load the CRM dashboard.");
      } finally {
        setIsLoading(false);
      }
    })();
  }, [accessToken]);

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <PageHeader title="CRM Overview" description="Current state of the sales pipeline, follow-ups, and recent activity." />
        <CrmNav />

        {error && <p role="alert">{error}</p>}

        {isLoading ? (
          <div className="stack" style={{ marginTop: "var(--space-2)" }}>
            <div className="skeleton skeleton-line" style={{ height: 90 }} />
            <div className="skeleton skeleton-line" style={{ height: 160 }} />
          </div>
        ) : (
          dashboard && (
            <>
              <section style={{ marginBottom: "var(--space-6)" }}>
                <div className="row">
                  <MetricCard label="Companies" value={dashboard.totalCompanies} />
                  <MetricCard label="Contacts" value={dashboard.totalContacts} />
                  <MetricCard label="Contacted This Month" value={dashboard.companiesContactedThisMonth} />
                  <MetricCard label="Needing Follow-up" value={dashboard.contactsAndCompaniesNeedingFollowUp} />
                </div>
              </section>

              <section style={{ marginBottom: "var(--space-6)" }}>
                <div className="row">
                  <MetricCard label="Open Deals" value={dashboard.openDeals} />
                  <MetricCard label="Won Deals" value={dashboard.wonDeals} />
                  <MetricCard label="Lost Deals" value={dashboard.lostDeals} />
                  <MetricCard
                    label="Open Pipeline Value"
                    value={`${dashboard.currency} ${dashboard.openPipelineValue.toLocaleString()}`}
                  />
                  <MetricCard
                    label="Weighted Pipeline (est.)"
                    value={`${dashboard.currency} ${Math.round(dashboard.weightedPipelineValue).toLocaleString()}`}
                  />
                </div>
              </section>

              <section style={{ marginBottom: "var(--space-8)" }}>
                <div className="row">
                  <MetricCard label="Tasks Due Today" value={dashboard.tasksDueToday} />
                  <MetricCard label="Overdue Tasks" value={dashboard.overdueTasks} />
                  <MetricCard label="Upcoming Follow-ups (7d)" value={dashboard.upcomingFollowUps} />
                </div>
              </section>

              <section>
                <h2>Recent Activity</h2>
                {dashboard.recentActivities.length === 0 ? (
                  <div className="empty-state">
                    <p className="empty-state__title">No Activity Yet</p>
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
                        {dashboard.recentActivities.map((activity) => {
                          const target = activityTargetLink(activity);
                          return (
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
                              <td data-label="Related to">{target ? <Link to={target.to}>{target.label}</Link> : "—"}</td>
                              <td data-label="Logged by">
                                {activity.loggedBy.firstName} {activity.loggedBy.lastName}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                )}
              </section>
            </>
          )
        )}
      </main>
    </div>
  );
}

export default CrmOverviewPage;
