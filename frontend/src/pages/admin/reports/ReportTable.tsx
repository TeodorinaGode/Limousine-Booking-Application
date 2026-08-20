import type { ReactNode } from "react";

interface ReportTableProps {
  title: string;
  isLoading: boolean;
  error: string | null;
  isEmpty: boolean;
  emptyMessage?: string;
  onExportCsv?: () => void;
  children: ReactNode;
}

/**
 * Consistent shell for every report table on the page: title, optional CSV
 * export button, and shared loading/empty/error states (section 31/51). The
 * table itself (thead/tbody) is read-only — no edit/cancel/assign buttons ever
 * belong here (section 55); use a Link to the booking detail page instead.
 */
function ReportTable({ title, isLoading, error, isEmpty, emptyMessage, onExportCsv, children }: ReportTableProps) {
  return (
    <section style={{ marginBottom: "1.5rem" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h2>{title}</h2>
        {onExportCsv && (
          <button type="button" onClick={onExportCsv} disabled={isLoading || isEmpty}>
            Export CSV
          </button>
        )}
      </div>
      {error && <p role="alert">{error}</p>}
      {!error && isLoading && <p>Loading...</p>}
      {!error && !isLoading && isEmpty && <p>{emptyMessage ?? "No data found for the selected period."}</p>}
      {!error && !isLoading && !isEmpty && (
        <table style={{ width: "100%", borderCollapse: "collapse" }}>{children}</table>
      )}
    </section>
  );
}

export default ReportTable;
