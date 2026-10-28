import { useDraggable } from "@dnd-kit/core";
import { CSS } from "@dnd-kit/utilities";
import { Link } from "react-router-dom";
import type { CrmDealDto } from "../../../../types/crmDeal";

interface DealCardProps {
  deal: CrmDealDto;
}

/** One draggable Kanban card — the whole card is the drag handle. */
function DealCard({ deal }: DealCardProps) {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({ id: deal.id });

  const style: React.CSSProperties = {
    transform: transform ? CSS.Translate.toString(transform) : undefined,
    opacity: isDragging ? 0.4 : 1,
    background: "var(--color-surface)",
    border: "1px solid var(--color-border-light)",
    borderRadius: "var(--radius-md)",
    padding: "var(--space-3)",
    marginBottom: "var(--space-2)",
    cursor: "grab",
    touchAction: "none",
  };

  return (
    <div ref={setNodeRef} style={style} {...listeners} {...attributes}>
      <Link to={`/admin/crm/deals/${deal.id}`} onClick={(e) => e.stopPropagation()} style={{ fontWeight: 600 }}>
        {deal.name}
      </Link>
      <div className="text-muted" style={{ fontSize: "0.85rem" }}>
        {deal.company.name}
      </div>
      <div style={{ marginTop: "var(--space-2)", fontWeight: 600 }}>
        {deal.value.toLocaleString()} {deal.currency}
      </div>
      <div className="text-muted" style={{ fontSize: "0.8rem" }}>
        {deal.probability}% probability
      </div>
    </div>
  );
}

export default DealCard;
