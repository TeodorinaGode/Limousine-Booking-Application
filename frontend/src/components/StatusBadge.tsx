const VARIANT_BY_STATUS: Record<string, string> = {
  pending: "pending",
  requiresmanualassignment: "warn",
  confirmed: "confirmed",
  assigned: "confirmed",
  upcoming: "confirmed",
  ontheway: "ontheway",
  passengerpickedup: "pickedup",
  completed: "completed",
  cancelled: "cancelled",
  inactive: "inactive",
  active: "active",
  available: "active",
  unavailable: "cancelled",
  automatic: "confirmed",
  manual: "warn",
  unassigned: "cancelled",
  failed: "cancelled",
  sent: "completed",
  retrying: "warn",
};

/** Renders any backend status string as a muted, monochrome badge — status is always conveyed by the text itself too, never color alone (section 46). */
function StatusBadge({ status }: { status: string }) {
  const variant = VARIANT_BY_STATUS[status.toLowerCase()] ?? "pending";
  return <span className={`badge badge--${variant}`}>{status}</span>;
}

export default StatusBadge;
