import type { ReactNode } from "react";

interface ChartCardProps {
  title: string;
  isLoading: boolean;
  error: string | null;
  isEmpty: boolean;
  emptyMessage?: string;
  children: ReactNode;
}

/** Consistent loading/empty/error handling around a chart (section 51) — never renders an unexplained empty chart. */
function ChartCard({ title, isLoading, error, isEmpty, emptyMessage, children }: ChartCardProps) {
  return (
    <section style={{ marginBottom: "1.5rem" }}>
      <h2>{title}</h2>
      {error && <p role="alert">{error}</p>}
      {!error && isLoading && <p>Loading...</p>}
      {!error && !isLoading && isEmpty && <p>{emptyMessage ?? "No data found for the selected period."}</p>}
      {!error && !isLoading && !isEmpty && children}
    </section>
  );
}

export default ChartCard;
