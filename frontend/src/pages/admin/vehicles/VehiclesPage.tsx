import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../../../context/AuthContext";
import {
  activateVehicle,
  createVehicle,
  deactivateVehicle,
  getVehicles,
  updateVehicle,
} from "../../../services/vehicleService";
import AdminNav from "../../../components/AdminNav";
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

  const [vehicles, setVehicles] = useState<VehicleDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState<ActiveFilter>("all");
  const [capacityFilter, setCapacityFilter] = useState<CapacityFilter>("All");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [modalState, setModalState] = useState<{ vehicle?: VehicleDto } | null>(null);

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

  useEffect(() => {
    if (!successMessage) return;
    const timeout = setTimeout(() => setSuccessMessage(null), 3000);
    return () => clearTimeout(timeout);
  }, [successMessage]);

  const handleSave = async (values: VehicleFormValues) => {
    if (!accessToken) return;

    const payload = { ...values, notes: values.notes.trim() === "" ? null : values.notes };

    if (modalState?.vehicle) {
      await updateVehicle(modalState.vehicle.id, payload, accessToken);
      setSuccessMessage("Vehicle updated successfully.");
    } else {
      await createVehicle(payload, accessToken);
      setSuccessMessage("Vehicle created successfully.");
    }

    setModalState(null);
    await loadVehicles();
  };

  const handleToggleActive = async (vehicle: VehicleDto) => {
    if (!accessToken) return;

    if (vehicle.isActive) {
      const confirmed = window.confirm("Are you sure you want to deactivate this vehicle?");
      if (!confirmed) return;
    }

    try {
      if (vehicle.isActive) {
        await deactivateVehicle(vehicle.id, accessToken);
        setSuccessMessage("Vehicle deactivated.");
      } else {
        await activateVehicle(vehicle.id, accessToken);
        setSuccessMessage("Vehicle activated.");
      }
      await loadVehicles();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the vehicle.");
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

      {successMessage && <p role="status">{successMessage}</p>}
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
        <div style={{ overflowX: "auto" }}>
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
                <td>{vehicle.registrationNumber}</td>
                <td>{vehicle.make}</td>
                <td>{vehicle.model}</td>
                <td>{vehicle.vehicleType}</td>
                <td>{vehicle.passengerCapacity}</td>
                <td>
                  <StatusBadge status={vehicle.isActive ? "Active" : "Inactive"} />
                </td>
                <td>
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
      </main>
    </div>
  );
}

export default VehiclesPage;
