import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../../../context/AuthContext";
import { getCrmReportSummary } from "../../../services/crmAnalyticsService";
import AdminNav from "../../../components/AdminNav";
import CrmNav from "../../../components/CrmNav";
import PageHeader from "../../../components/PageHeader";
import ChartCard from "../reports/ChartCard";
import DateRangeFilter from "../reports/DateRangeFilter";
import MetricCard from "../reports/MetricCard";
import BarChart from "../reports/BarChart";
import type { CrmReportSummaryDto } from "../../../types/crmAnalytics";

function zurichToday(): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Zurich" }).format(new Date());
}

function startOfZurichMonth(): string {
  const today = new Date(`${zurichToday()}T00:00:00`);
  return new Date(today.getFullYear(), today.getMonth(), 1).toISOString().slice(0, 10);
}

function CrmReportsPage() {
  const { accessToken } = useAuth();

  const [dateFrom, setDateFrom] = useState(startOfZurichMonth());
  const [dateTo, setDateTo] = useState(zurichToday());
  const [summary, setSummary] = useState<CrmReportSummaryDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadSummary = useCallback(async () => {
    if (!accessToken) return;
    setIsLoading(true);
    setError(null);
    try {
      setSummary(await getCrmReportSummary({ dateFrom, dateTo }, accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the CRM report.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, dateFrom, dateTo]);

  useEffect(() => {
    loadSummary();
  }, [loadSummary]);

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <PageHeader title="CRM Reports" description="Date-ranged analysis of new companies/contacts, outreach, and deal outcomes." />
        <CrmNav />

        <DateRangeFilter dateFrom={dateFrom} dateTo={dateTo} onChange={(from, to) => { setDateFrom(from); setDateTo(to); }} />

        {error && <p role="alert">{error}</p>}

        {!error && summary && (
          <>
            <section style={{ marginBottom: "var(--space-6)" }}>
              <div className="row">
                <MetricCard label="New Companies" value={summary.newCompanies} />
                <MetricCard label="New Contacts" value={summary.newContacts} />
                <MetricCard label="Activities Logged" value={summary.activitiesLogged} />
                <MetricCard label="Tasks Completed" value={summary.tasksCompleted} />
              </div>
            </section>

            <section style={{ marginBottom: "var(--space-6)" }}>
              <div className="row">
                <MetricCard label="Deals Won" value={summary.dealsWon} />
                <MetricCard label="Deals Lost" value={summary.dealsLost} />
                <MetricCard label="Won Value" value={`${summary.currency} ${summary.wonValue.toLocaleString()}`} />
                <MetricCard
                  label="Conversion Rate"
                  value={summary.conversionRatePercent === null ? "No closed deals in range" : `${summary.conversionRatePercent}%`}
                />
              </div>
            </section>

            <ChartCard
              title="Open Pipeline by Stage (current snapshot)"
              isLoading={isLoading}
              error={null}
              isEmpty={summary.openDealsByStage.length === 0}
              emptyMessage="No open deals in the pipeline right now."
            >
              <BarChart
                series={[{ name: "Deal count", color: "var(--color-accent-strong)" }]}
                data={summary.openDealsByStage.map((item) => ({ label: item.stageName, values: [item.dealCount] }))}
                height={180}
              />
            </ChartCard>
          </>
        )}
      </main>
    </div>
  );
}

export default CrmReportsPage;
