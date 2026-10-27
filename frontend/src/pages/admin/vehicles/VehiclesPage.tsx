import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../../../context/AuthContext";
import { useToast } from "../../../context/ToastContext";
import {
  activateVehicle,
  createVehicle,
  deactivateVehicle,
  getVehicles,
  updateVehicle,
} from "../../../services/vehicleService";
import AdminNav from "../../../components/AdminNav";
import ConfirmDialog from "../../../components/ConfirmDialog";
import PageHeader from "../../../components/PageHeader";
import StatusBadge from "../../../components/StatusBadge";
import type { ActiveFilter, VehicleDto } from "../../../types/vehicle";
import VehicleFormModal, { type VehicleFormValues } from "./VehicleFormModal";

const PAGE_SIZE = 20;
const CAPACITY_FILTER_OPTIONS = ["All", "1+", "3+", "5+", "7+"] as const;
type CapacityFilter = (typeof CAPACITY_FILTER_OPTIONS)[number];

function toIsActive(filter: ActiveFilter): boolean | undefined {
  if (filter === "active") return true;
  if (filter === "inactive") return false;
  return undefined;
}

function toMinCapacity(filter: CapacityFilter): number | undefined {
  return filter === "All" ? undefined : Number(filter.replace("+", ""));
}

function VehiclesPage() {
  const { accessToken } = useAuth();
  const { showToast } = useToast();

  const [vehicles, setVehicles] = useState<VehicleDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState<ActiveFilter>("all");
  const [capacityFilter, setCapacityFilter] = useState<CapacityFilter>("All");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modalState, setModalState] = useState<{ vehicle?: VehicleDto } | null>(null);
  const [deactivateTarget, setDeactivateTarget] = useState<VehicleDto | null>(null);
  const [isTogglingActive, setIsTogglingActive] = useState(false);

  const loadVehicles = useCallback(async () => {
    if (!accessToken) return;

    setIsLoading(true);
    setError(null);
    try {
      const result = await getVehicles(
        {
          search,
          isActive: toIsActive(activeFilter),
          minCapacity: toMinCapacity(capacityFilter),
          page,
          pageSize: PAGE_SIZE,
        },
        accessToken
      );
      setVehicles(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load vehicles.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, search, activeFilter, capacityFilter, page]);

  useEffect(() => {
    loadVehicles();
  }, [loadVehicles]);

  // Debounce the search box so we don't hit the API on every keystroke.
  useEffect(() => {
    const timeout = setTimeout(() => {
      setPage(1);
      setSearch(searchInput);
    }, 300);
    return () => clearTimeout(timeout);
  }, [searchInput]);

  const handleSave = async (values: VehicleFormValues) => {
    if (!accessToken) return;

    const payload = { ...values, notes: values.notes.trim() === "" ? null : values.notes };

    if (modalState?.vehicle) {
      await updateVehicle(modalState.vehicle.id, payload, accessToken);
      showToast("Vehicle updated successfully.");
    } else {
      await createVehicle(payload, accessToken);
      showToast("Vehicle created successfully.");
    }

    setModalState(null);
    await loadVehicles();
  };

  const handleToggleActive = async (vehicle: VehicleDto) => {
    if (!accessToken) return;

    if (vehicle.isActive) {
      setDeactivateTarget(vehicle);
      return;
    }

    try {
      await activateVehicle(vehicle.id, accessToken);
      showToast("Vehicle activated.");
      await loadVehicles();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the vehicle.");
    }
  };

  const handleConfirmDeactivate = async () => {
    if (!accessToken || !deactivateTarget) return;

    setIsTogglingActive(true);
    try {
      await deactivateVehicle(deactivateTarget.id, accessToken);
      showToast("Vehicle deactivated.");
      await loadVehicles();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the vehicle.");
    } finally {
      setIsTogglingActive(false);
      setDeactivateTarget(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
      <PageHeader
        title="Vehicles"
        description="Manage the fleet's active vehicles and capacity."
        actions={
          <button type="button" onClick={() => setModalState({})}>
            + New Vehicle
          </button>
        }
      />

      <div className="row" style={{ marginBottom: "1rem" }}>
        <input
          type="search"
          placeholder="Search vehicles..."
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          aria-label="Search vehicles"
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

        <label>
          Capacity:{" "}
          <select
            value={capacityFilter}
            onChange={(e) => {
              setPage(1);
              setCapacityFilter(e.target.value as CapacityFilter);
            }}
          >
            {CAPACITY_FILTER_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </label>
      </div>

      {error && <p role="alert">{error}</p>}

      {isLoading ? (
        <div className="stack">
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
        </div>
      ) : vehicles.length === 0 ? (
        <div className="empty-state">
          <p className="empty-state__title">No Vehicles Found</p>
          <p>Try adjusting your filters.</p>
        </div>
      ) : (
        <div className="table-responsive" style={{ overflowX: "auto" }}>
        <table>
          <thead>
            <tr>
              <th>Registration</th>
              <th>Make</th>
              <th>Model</th>
              <th>Type</th>
              <th>Capacity</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {vehicles.map((vehicle) => (
              <tr key={vehicle.id}>
                <td data-label="Registration">{vehicle.registrationNumber}</td>
                <td data-label="Make">{vehicle.make}</td>
                <td data-label="Model">{vehicle.model}</td>
                <td data-label="Type">{vehicle.vehicleType}</td>
                <td data-label="Capacity">{vehicle.passengerCapacity}</td>
                <td data-label="Status">
                  <StatusBadge status={vehicle.isActive ? "Active" : "Inactive"} />
                </td>
                <td data-label="Actions">
                  <div className="row" style={{ gap: "var(--space-2)" }}>
                    <button type="button" className="btn-ghost" onClick={() => setModalState({ vehicle })}>
                      Edit
                    </button>
                    <button type="button" className="btn-ghost" onClick={() => handleToggleActive(vehicle)}>
                      {vehicle.isActive ? "Deactivate" : "Activate"}
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
        <VehicleFormModal vehicle={modalState.vehicle} onSave={handleSave} onClose={() => setModalState(null)} />
      )}

      {deactivateTarget && (
        <ConfirmDialog
          title="Deactivate Vehicle"
          message={`Deactivate ${deactivateTarget.make} ${deactivateTarget.model} (${deactivateTarget.registrationNumber})? It will no longer be eligible for new bookings.`}
          confirmLabel="Deactivate"
          danger
          isBusy={isTogglingActive}
          onConfirm={handleConfirmDeactivate}
          onCancel={() => setDeactivateTarget(null)}
        />
      )}
      </main>
    </div>
  );
}

export default VehiclesPage;
