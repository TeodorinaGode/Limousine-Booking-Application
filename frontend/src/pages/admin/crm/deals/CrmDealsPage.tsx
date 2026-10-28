import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../../../context/AuthContext";
import { useToast } from "../../../../context/ToastContext";
import { getAdministrators } from "../../../../services/administratorService";
import { getCrmCompanies } from "../../../../services/crmCompanyService";
import { createCrmDeal, getCrmDealBoard, getCrmDeals, moveCrmDealStage } from "../../../../services/crmDealService";
import AdminNav from "../../../../components/AdminNav";
import CrmNav from "../../../../components/CrmNav";
import ConfirmDialog from "../../../../components/ConfirmDialog";
import PageHeader from "../../../../components/PageHeader";
import StatusBadge from "../../../../components/StatusBadge";
import type { AdministratorSummaryDto } from "../../../../types/administrator";
import type { CrmCompanyDto } from "../../../../types/crmCompany";
import type { CrmDealDto, CrmPipelineBoardDto } from "../../../../types/crmDeal";
import CrmDealFormModal, { type CrmDealFormValues } from "./CrmDealFormModal";
import DealKanbanBoard from "./DealKanbanBoard";

const PAGE_SIZE = 20;

type ViewMode = "kanban" | "table";

function CrmDealsPage() {
  const { accessToken } = useAuth();
  const { showToast } = useToast();

  const [viewMode, setViewMode] = useState<ViewMode>("kanban");
  const [board, setBoard] = useState<CrmPipelineBoardDto | null>(null);
  const [isBoardLoading, setIsBoardLoading] = useState(true);

  const [deals, setDeals] = useState<CrmDealDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isTableLoading, setIsTableLoading] = useState(true);

  const [error, setError] = useState<string | null>(null);
  const [companies, setCompanies] = useState<CrmCompanyDto[]>([]);
  const [administrators, setAdministrators] = useState<AdministratorSummaryDto[]>([]);
  const [isFormOpen, setIsFormOpen] = useState(false);

  const [pendingLostMove, setPendingLostMove] = useState<{ dealId: string; fromStageId: string; toStageId: string } | null>(null);
  const [isMovingStage, setIsMovingStage] = useState(false);

  const loadBoard = useCallback(async () => {
    if (!accessToken) return;
    setIsBoardLoading(true);
    setError(null);
    try {
      setBoard(await getCrmDealBoard(accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the pipeline board.");
    } finally {
      setIsBoardLoading(false);
    }
  }, [accessToken]);

  const loadDeals = useCallback(async () => {
    if (!accessToken) return;
    setIsTableLoading(true);
    setError(null);
    try {
      const result = await getCrmDeals({ page, pageSize: PAGE_SIZE }, accessToken);
      setDeals(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load deals.");
    } finally {
      setIsTableLoading(false);
    }
  }, [accessToken, page]);

  const loadLookups = useCallback(async () => {
    if (!accessToken) return;
    try {
      const [companyResult, adminResult] = await Promise.all([
        getCrmCompanies({ pageSize: 100 }, accessToken),
        getAdministrators(accessToken),
      ]);
      setCompanies(companyResult.items);
      setAdministrators(adminResult);
    } catch {
      // Non-fatal — the create-deal form's dropdowns just stay empty.
    }
  }, [accessToken]);

  useEffect(() => {
    if (viewMode === "kanban") loadBoard();
    else loadDeals();
  }, [viewMode, loadBoard, loadDeals]);

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  const handleCreate = async (values: CrmDealFormValues) => {
    if (!accessToken) return;

    await createCrmDeal(
      {
        name: values.name,
        companyId: values.companyId,
        description: values.description || null,
        dealType: values.dealType || null,
        value: Number(values.value),
        currency: values.currency,
        expectedCloseDate: values.expectedCloseDate || null,
        ownerId: values.ownerId || null,
        nextAction: values.nextAction || null,
        notes: values.notes || null,
      },
      accessToken
    );
    showToast("Deal created successfully.");
    setIsFormOpen(false);
    if (viewMode === "kanban") await loadBoard();
    else await loadDeals();
  };

  /** Optimistically moves a deal between columns in local state; returns a rollback function. */
  const applyOptimisticMove = (dealId: string, fromStageId: string, toStageId: string) => {
    if (!board) return () => {};

    const previousBoard = board;
    const fromColumn = board.columns.find((c) => c.stage.id === fromStageId);
    const toColumn = board.columns.find((c) => c.stage.id === toStageId);
    const deal = fromColumn?.deals.find((d) => d.id === dealId);
    if (!fromColumn || !toColumn || !deal) return () => {};

    const movedDeal: CrmDealDto = {
      ...deal,
      stageId: toColumn.stage.id,
      stageName: toColumn.stage.name,
      isWon: toColumn.stage.isWon,
      isLost: toColumn.stage.isLost,
      probability: toColumn.stage.defaultProbability,
    };

    setBoard({
      ...board,
      columns: board.columns.map((column) => {
        if (column.stage.id === fromStageId) return { ...column, deals: column.deals.filter((d) => d.id !== dealId) };
        if (column.stage.id === toStageId) return { ...column, deals: [...column.deals, movedDeal] };
        return column;
      }),
    });

    return () => setBoard(previousBoard);
  };

  const commitMove = async (dealId: string, toStageId: string, lossReason?: string) => {
    if (!accessToken) return;
    try {
      await moveCrmDealStage(dealId, { stageId: toStageId, lossReason: lossReason ?? null }, accessToken);
    } catch (err) {
      throw err instanceof Error ? err : new Error("Failed to move the deal.");
    }
  };

  const handleDealDropped = async (dealId: string, fromStageId: string, toStageId: string) => {
    if (!board) return;

    const toColumn = board.columns.find((c) => c.stage.id === toStageId);
    if (!toColumn) return;

    if (toColumn.stage.isLost) {
      // Moving into a Lost stage requires a reason before committing — the
      // card stays in its original column until the admin confirms or cancels.
      setPendingLostMove({ dealId, fromStageId, toStageId });
      return;
    }

    const rollback = applyOptimisticMove(dealId, fromStageId, toStageId);
    try {
      await commitMove(dealId, toStageId);
    } catch (err) {
      rollback();
      showToast(err instanceof Error ? err.message : "Failed to move the deal.", "error");
    }
  };

  const handleConfirmLostMove = async (reason?: string) => {
    if (!pendingLostMove || !reason) {
      setPendingLostMove(null);
      return;
    }

    const { dealId, fromStageId, toStageId } = pendingLostMove;
    setIsMovingStage(true);
    const rollback = applyOptimisticMove(dealId, fromStageId, toStageId);
    try {
      await commitMove(dealId, toStageId, reason);
    } catch (err) {
      rollback();
      showToast(err instanceof Error ? err.message : "Failed to move the deal.", "error");
    } finally {
      setIsMovingStage(false);
      setPendingLostMove(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
        <PageHeader
          title="CRM — Deals"
          description="Potential business opportunities, such as annual corporate chauffeur contracts."
          actions={
            <button type="button" onClick={() => setIsFormOpen(true)}>
              + New Deal
            </button>
          }
        />
        <CrmNav />

        <div className="row" style={{ marginBottom: "1rem" }}>
          <button type="button" className={viewMode === "kanban" ? "btn-secondary" : "btn-ghost"} onClick={() => setViewMode("kanban")}>
            Kanban
          </button>
          <button type="button" className={viewMode === "table" ? "btn-secondary" : "btn-ghost"} onClick={() => setViewMode("table")}>
            Table
          </button>
        </div>

        {error && <p role="alert">{error}</p>}

        {viewMode === "kanban" ? (
          isBoardLoading ? (
            <div className="skeleton skeleton-line" style={{ height: 200 }} />
          ) : board && board.columns.length > 0 ? (
            <DealKanbanBoard columns={board.columns} onDealDropped={handleDealDropped} />
          ) : (
            <div className="empty-state">
              <p className="empty-state__title">No Pipeline Configured</p>
            </div>
          )
        ) : isTableLoading ? (
          <div className="stack">
            <div className="skeleton skeleton-line" style={{ height: 40 }} />
            <div className="skeleton skeleton-line" style={{ height: 40 }} />
          </div>
        ) : deals.length === 0 ? (
          <div className="empty-state">
            <p className="empty-state__title">No Deals Found</p>
          </div>
        ) : (
          <>
            <div className="table-responsive" style={{ overflowX: "auto" }}>
              <table>
                <thead>
                  <tr>
                    <th>Deal</th>
                    <th>Company</th>
                    <th>Stage</th>
                    <th>Value</th>
                    <th>Probability</th>
                    <th>Owner</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {deals.map((deal) => (
                    <tr key={deal.id}>
                      <td data-label="Deal">{deal.name}</td>
                      <td data-label="Company">{deal.company.name}</td>
                      <td data-label="Stage">
                        <StatusBadge status={deal.isWon ? "Completed" : deal.isLost ? "Cancelled" : deal.stageName} />
                      </td>
                      <td data-label="Value">
                        {deal.value.toLocaleString()} {deal.currency}
                      </td>
                      <td data-label="Probability">{deal.probability}%</td>
                      <td data-label="Owner">{deal.owner ? `${deal.owner.firstName} ${deal.owner.lastName}` : "Unassigned"}</td>
                      <td data-label="Actions">
                        <Link to={`/admin/crm/deals/${deal.id}`}>View Details</Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
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
          </>
        )}

        {isFormOpen && (
          <CrmDealFormModal
            companies={companies}
            administrators={administrators}
            onSave={handleCreate}
            onClose={() => setIsFormOpen(false)}
          />
        )}

        {pendingLostMove && (
          <ConfirmDialog
            title="Mark Deal as Lost"
            message="Moving this deal to Closed Lost requires a reason. This helps track why opportunities don't close."
            confirmLabel="Mark as Lost"
            reasonLabel="Loss reason (required)"
            danger
            isBusy={isMovingStage}
            onConfirm={handleConfirmLostMove}
            onCancel={() => setPendingLostMove(null)}
          />
        )}
      </main>
    </div>
  );
}

export default CrmDealsPage;
