import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../../../context/AuthContext";
import {
  activateRoute,
  createRoute,
  deactivateRoute,
  getRoutes,
  updateRoute,
} from "../../../services/routeService";
import AdminNav from "../../../components/AdminNav";
import PageHeader from "../../../components/PageHeader";
import StatusBadge from "../../../components/StatusBadge";
import type { ActiveFilter, RouteDto } from "../../../types/route";
import RouteFormModal, { type RouteFormValues } from "./RouteFormModal";

const PAGE_SIZE = 20;

function toIsActive(filter: ActiveFilter): boolean | undefined {
  if (filter === "active") return true;
  if (filter === "inactive") return false;
  return undefined;
}

function RoutesPage() {
  const { accessToken } = useAuth();

  const [routes, setRoutes] = useState<RouteDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState<ActiveFilter>("all");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [modalState, setModalState] = useState<{ route?: RouteDto } | null>(null);

  const loadRoutes = useCallback(async () => {
    if (!accessToken) return;

    setIsLoading(true);
    setError(null);
    try {
      const result = await getRoutes(
        { search, isActive: toIsActive(activeFilter), page, pageSize: PAGE_SIZE },
        accessToken
      );
      setRoutes(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load routes.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, search, activeFilter, page]);

  useEffect(() => {
    loadRoutes();
  }, [loadRoutes]);

  // Debounce the search box so we don't hit the API on every keystroke.
  useEffect(() => {
    const timeout = setTimeout(() => {
      setPage(1);
      setSearch(searchInput);
    }, 300);
    return () => clearTimeout(timeout);
  }, [searchInput]);

  useEffect(() => {
    if (!successMessage) return;
    const timeout = setTimeout(() => setSuccessMessage(null), 3000);
    return () => clearTimeout(timeout);
  }, [successMessage]);

  const handleSave = async (values: RouteFormValues) => {
    if (!accessToken) return;

    if (modalState?.route) {
      await updateRoute(modalState.route.id, values, accessToken);
      setSuccessMessage("Route updated successfully.");
    } else {
      await createRoute(values, accessToken);
      setSuccessMessage("Route created successfully.");
    }

    setModalState(null);
    await loadRoutes();
  };

  const handleToggleActive = async (route: RouteDto) => {
    if (!accessToken) return;

    if (route.isActive) {
      const confirmed = window.confirm("Are you sure you want to deactivate this route?");
      if (!confirmed) return;
    }

    try {
      if (route.isActive) {
        await deactivateRoute(route.id, accessToken);
        setSuccessMessage("Route deactivated.");
      } else {
        await activateRoute(route.id, accessToken);
        setSuccessMessage("Route activated.");
      }
      await loadRoutes();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the route.");
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
      <PageHeader
        title="Routes"
        description="Manage the predefined trips customers can book."
        actions={
          <button type="button" onClick={() => setModalState({})}>
            + New Route
          </button>
        }
      />

      <div className="row" style={{ marginBottom: "1rem" }}>
        <input
          type="search"
          placeholder="Search routes..."
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          aria-label="Search routes"
        />

        <label>
          Status:{" "}
          <select
            value={activeFilter}
            onChange={(e) => {
              setPage(1);
              setActiveFilter(e.target.value as ActiveFilter);
            }}
          >
            <option value="all">All</option>
            <option value="active">Active</option>
            <option value="inactive">Inactive</option>
          </select>
        </label>
      </div>

      {successMessage && <p role="status">{successMessage}</p>}
      {error && <p role="alert">{error}</p>}

      {isLoading ? (
        <div className="stack">
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
        </div>
      ) : routes.length === 0 ? (
        <div className="empty-state">
          <p className="empty-state__title">No Routes Found</p>
          <p>Try adjusting your filters.</p>
        </div>
      ) : (
        <div style={{ overflowX: "auto" }}>
        <table>
          <thead>
            <tr>
              <th>Departure</th>
              <th>Destination</th>
              <th>Duration</th>
              <th>Price</th>
              <th>Currency</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {routes.map((route) => (
              <tr key={route.id}>
                <td>{route.departureLocation}</td>
                <td>{route.destination}</td>
                <td>{route.estimatedDurationMinutes} min</td>
                <td>{route.price.toFixed(2)}</td>
                <td>{route.currency}</td>
                <td>
                  <StatusBadge status={route.isActive ? "Active" : "Inactive"} />
                </td>
                <td>
                  <div className="row" style={{ gap: "var(--space-2)" }}>
                    <button type="button" className="btn-ghost" onClick={() => setModalState({ route })}>
                      Edit
                    </button>
                    <button type="button" className="btn-ghost" onClick={() => handleToggleActive(route)}>
                      {route.isActive ? "Deactivate" : "Activate"}
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>
      )}

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

      {modalState && (
        <RouteFormModal route={modalState.route} onSave={handleSave} onClose={() => setModalState(null)} />
      )}
      </main>
    </div>
  );
}

export default RoutesPage;
