/** Today's date in Europe/Zurich, as YYYY-MM-DD — presets must match the backend's business timezone, not the browser's. */
function zurichToday(): Date {
  const parts = new Intl.DateTimeFormat("en-CA", { timeZone: "Europe/Zurich" }).format(new Date());
  return new Date(`${parts}T00:00:00`);
}

function toIso(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function addDays(date: Date, days: number): Date {
  const copy = new Date(date);
  copy.setDate(copy.getDate() + days);
  return copy;
}

const PRESETS: { label: string; range: () => [string, string] }[] = [
  { label: "Today", range: () => [toIso(zurichToday()), toIso(zurichToday())] },
  { label: "Yesterday", range: () => [toIso(addDays(zurichToday(), -1)), toIso(addDays(zurichToday(), -1))] },
  {
    label: "This week",
    range: () => {
      const today = zurichToday();
      const day = today.getDay();
      const start = addDays(today, day === 0 ? -6 : 1 - day);
      return [toIso(start), toIso(today)];
    },
  },
  { label: "Last 7 days", range: () => [toIso(addDays(zurichToday(), -6)), toIso(zurichToday())] },
  {
    label: "This month",
    range: () => {
      const today = zurichToday();
      return [toIso(new Date(today.getFullYear(), today.getMonth(), 1)), toIso(today)];
    },
  },
  {
    label: "Last month",
    range: () => {
      const today = zurichToday();
      const start = new Date(today.getFullYear(), today.getMonth() - 1, 1);
      const end = new Date(today.getFullYear(), today.getMonth(), 0);
      return [toIso(start), toIso(end)];
    },
  },
  {
    label: "This year",
    range: () => {
      const today = zurichToday();
      return [toIso(new Date(today.getFullYear(), 0, 1)), toIso(today)];
    },
  },
];

interface DateRangeFilterProps {
  dateFrom: string;
  dateTo: string;
  onChange: (dateFrom: string, dateTo: string) => void;
}

/** The one global report date filter (section 52) — every report card/chart on the page reads from the same dateFrom/dateTo state. */
function DateRangeFilter({ dateFrom, dateTo, onChange }: DateRangeFilterProps) {
  return (
    <div style={{ display: "flex", gap: "0.5rem", alignItems: "center", flexWrap: "wrap", marginBottom: "1rem" }}>
      {PRESETS.map((preset) => (
        <button key={preset.label} type="button" onClick={() => onChange(...preset.range())}>
          {preset.label}
        </button>
      ))}
      <label>
        From:{" "}
        <input type="date" aria-label="Date from" value={dateFrom} onChange={(e) => onChange(e.target.value, dateTo)} />
      </label>
      <label>
        To:{" "}
        <input type="date" aria-label="Date to" value={dateTo} onChange={(e) => onChange(dateFrom, e.target.value)} />
      </label>
    </div>
  );
}

export default DateRangeFilter;
