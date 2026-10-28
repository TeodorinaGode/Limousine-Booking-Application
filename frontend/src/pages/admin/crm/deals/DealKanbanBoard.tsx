import { DndContext, type DragEndEvent, useDroppable } from "@dnd-kit/core";
import type { CrmPipelineBoardColumnDto } from "../../../../types/crmDeal";
import DealCard from "./DealCard";

interface KanbanColumnProps {
  column: CrmPipelineBoardColumnDto;
}

function KanbanColumn({ column }: KanbanColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id: column.stage.id });

  const totalValue = column.deals.reduce((sum, deal) => sum + deal.value, 0);

  return (
    <div
      ref={setNodeRef}
      style={{
        flex: "0 0 260px",
        display: "flex",
        flexDirection: "column",
        background: isOver ? "var(--color-surface-hover, var(--color-surface))" : "transparent",
        border: "1px dashed var(--color-border-light)",
        borderRadius: "var(--radius-md)",
        padding: "var(--space-3)",
        minHeight: 200,
        maxHeight: "calc(100vh - 340px)",
      }}
    >
      <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "var(--space-3)" }}>
        <strong>{column.stage.name}</strong>
        <span className="text-muted">{column.deals.length}</span>
      </div>
      <div className="text-muted" style={{ fontSize: "0.8rem", marginBottom: "var(--space-3)" }}>
        {totalValue.toLocaleString()} total
      </div>
      {/* Each column scrolls its own card list independently — the board
          itself only ever scrolls horizontally (see the row below). */}
      <div style={{ overflowY: "auto", flex: 1 }}>
        {column.deals.map((deal) => (
          <DealCard key={deal.id} deal={deal} />
        ))}
      </div>
    </div>
  );
}

interface DealKanbanBoardProps {
  columns: CrmPipelineBoardColumnDto[];
  onDealDropped: (dealId: string, fromStageId: string, toStageId: string) => void;
}

/** Kanban board for the deal pipeline — one column per stage, cards draggable between columns. */
function DealKanbanBoard({ columns, onDealDropped }: DealKanbanBoardProps) {
  const handleDragEnd = (event: DragEndEvent) => {
    const dealId = String(event.active.id);
    const toStageId = event.over?.id ? String(event.over.id) : null;
    if (!toStageId) return;

    const fromColumn = columns.find((c) => c.deals.some((d) => d.id === dealId));
    if (!fromColumn || fromColumn.stage.id === toStageId) return;

    onDealDropped(dealId, fromColumn.stage.id, toStageId);
  };

  return (
    <DndContext onDragEnd={handleDragEnd}>
      {/* Not a table — was previously borrowing .table-responsive just for its
          overflow-x:auto, which also forces the browser to compute overflow-y
          as auto (an element can't have one axis "auto" and the other
          "visible"), trapping the whole board in an extra vertical scroll
          region. Each column now scrolls itself, so this row only ever needs
          horizontal scroll. */}
      <div style={{ display: "flex", gap: "var(--space-3)", overflowX: "auto", overflowY: "hidden", paddingBottom: "var(--space-3)" }}>
        {columns.map((column) => (
          <KanbanColumn key={column.stage.id} column={column} />
        ))}
      </div>
    </DndContext>
  );
}

export default DealKanbanBoard;
