import { useTranslation } from "react-i18next";

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
  notstarted: "pending",
  processing: "warn",
  paid: "completed",
  refunded: "warn",
};

/** Which of common.json's status.* groups this value belongs to — booking/ride/payment lifecycles use a few overlapping raw values (e.g. "Cancelled", "Completed") with the same translated wording, so trying each group in turn (rather than requiring the caller to pick exactly one) keeps this component usable from any status column without per-call-site plumbing. */
const STATUS_GROUPS = ["booking", "ride", "payment"] as const;

/** Renders any backend status string as a muted, monochrome badge — status is always conveyed by the text itself too, never color alone (section 46). The raw value (e.g. "PassengerPickedUp") is never shown directly to the customer/admin/driver (section 27) — it's translated via common.json's status.* tables, falling back to the raw value only if no group has a matching key. */
function StatusBadge({ status }: { status: string }) {
  const { t, i18n } = useTranslation("common");
  const variant = VARIANT_BY_STATUS[status.toLowerCase()] ?? "pending";

  const group = STATUS_GROUPS.find((g) => i18n.exists(`common:status.${g}.${status}`));
  const label = group ? t(`status.${group}.${status}`) : status;

  return <span className={`badge badge--${variant}`}>{label}</span>;
}

export default StatusBadge;
