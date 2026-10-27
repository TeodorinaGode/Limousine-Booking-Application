import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../../../context/AuthContext";
import { useToast } from "../../../context/ToastContext";
import {
  activateDriver,
  createDriver,
  deactivateDriver,
  getDrivers,
  resetDriverPassword,
  updateDriver,
} from "../../../services/driverService";
import { getVehicles } from "../../../services/vehicleService";
import AdminNav from "../../../components/AdminNav";
import ConfirmDialog from "../../../components/ConfirmDialog";
import PageHeader from "../../../components/PageHeader";
import StatusBadge from "../../../components/StatusBadge";
import type { ActiveFilter, AvailabilityFilter, DriverDto, VehicleFilter } from "../../../types/driver";
import type { VehicleDto } from "../../../types/vehicle";
import DriverFormModal, { type DriverFormValues } from "./DriverFormModal";
import ResetPasswordModal from "./ResetPasswordModal";

const PAGE_SIZE = 20;

function toIsActive(filter: ActiveFilter): boolean | undefined {
  if (filter === "active") return true;
  if (filter === "inactive") return false;
  return undefined;
}

function toIsAvailable(filter: AvailabilityFilter): boolean | undefined {
  if (filter === "available") return true;
  if (filter === "unavailable") return false;
  return undefined;
}

function toHasVehicle(filter: VehicleFilter): boolean | undefined {
  if (filter === "assigned") return true;
  if (filter === "unassigned") return false;
  return undefined;
}

function DriversPage() {
  const { accessToken } = useAuth();
  const { showToast } = useToast();

  const [drivers, setDrivers] = useState<DriverDto[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState<ActiveFilter>("all");
  const [availabilityFilter, setAvailabilityFilter] = useState<AvailabilityFilter>("all");
  const [vehicleFilter, setVehicleFilter] = useState<VehicleFilter>("all");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modalState, setModalState] = useState<{ driver?: DriverDto } | null>(null);
  const [passwordResetDriver, setPasswordResetDriver] = useState<DriverDto | null>(null);
  const [activeVehicles, setActiveVehicles] = useState<VehicleDto[]>([]);
  const [deactivateTarget, setDeactivateTarget] = useState<DriverDto | null>(null);
  const [isTogglingActive, setIsTogglingActive] = useState(false);

  const loadDrivers = useCallback(async () => {
    if (!accessToken) return;

    setIsLoading(true);
    setError(null);
    try {
      const result = await getDrivers(
        {
          search,
          isActive: toIsActive(activeFilter),
          isAvailable: toIsAvailable(availabilityFilter),
          hasVehicle: toHasVehicle(vehicleFilter),
          page,
          pageSize: PAGE_SIZE,
        },
        accessToken
      );
      setDrivers(result.items);
      setTotalCount(result.totalCount);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load drivers.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, search, activeFilter, availabilityFilter, vehicleFilter, page]);

  const loadActiveVehicles = useCallback(async () => {
    if (!accessToken) return;
    try {
      const result = await getVehicles({ isActive: true, pageSize: 100 }, accessToken);
      setActiveVehicles(result.items);
    } catch {
      // Non-fatal — the vehicle dropdown just falls back to "No vehicle" only.
    }
  }, [accessToken]);

  useEffect(() => {
    loadDrivers();
  }, [loadDrivers]);

  useEffect(() => {
    loadActiveVehicles();
  }, [loadActiveVehicles]);

  // Debounce the search box so we don't hit the API on every keystroke.
  useEffect(() => {
    const timeout = setTimeout(() => {
      setPage(1);
      setSearch(searchInput);
    }, 300);
    return () => clearTimeout(timeout);
  }, [searchInput]);

  const handleSave = async (values: DriverFormValues) => {
    if (!accessToken) return;

    if (modalState?.driver) {
      await updateDriver(
        modalState.driver.id,
        {
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          phone: values.phone,
          isActive: values.isActive,
          vehicleId: values.vehicleId || null,
        },
        accessToken
      );
      showToast("Driver updated successfully.");
    } else {
      await createDriver(
        {
          firstName: values.firstName,
          lastName: values.lastName,
          email: values.email,
          phone: values.phone,
          password: values.password,
          vehicleId: values.vehicleId || null,
        },
        accessToken
      );
      showToast("Driver created successfully.");
    }

    setModalState(null);
    await loadDrivers();
    await loadActiveVehicles();
  };

  const handleToggleActive = async (driver: DriverDto) => {
    if (!accessToken) return;

    if (driver.isActive) {
      setDeactivateTarget(driver);
      return;
    }

    try {
      await activateDriver(driver.id, accessToken);
      showToast("Driver activated.");
      await loadDrivers();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the driver.");
    }
  };

  const handleConfirmDeactivate = async () => {
    if (!accessToken || !deactivateTarget) return;

    setIsTogglingActive(true);
    try {
      await deactivateDriver(deactivateTarget.id, accessToken);
      showToast("Driver deactivated.");
      await loadDrivers();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the driver.");
    } finally {
      setIsTogglingActive(false);
      setDeactivateTarget(null);
    }
  };

  const handleResetPassword = async (newPassword: string) => {
    if (!accessToken || !passwordResetDriver) return;

    await resetDriverPassword(passwordResetDriver.id, { newPassword }, accessToken);
    setPasswordResetDriver(null);
    showToast("Password reset successfully.");
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
      <PageHeader
        title="Drivers"
        description="Manage driver accounts, availability, and vehicle assignment."
        actions={
          <button type="button" onClick={() => setModalState({})}>
            + New Driver
          </button>
        }
      />

      <div className="row" style={{ marginBottom: "1rem" }}>
        <input
          type="search"
          placeholder="Search drivers..."
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          aria-label="Search drivers"
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
          Availability:{" "}
          <select
            value={availabilityFilter}
            onChange={(e) => {
              setPage(1);
              setAvailabilityFilter(e.target.value as AvailabilityFilter);
            }}
          >
            <option value="all">All</option>
            <option value="available">Available</option>
            <option value="unavailable">Unavailable</option>
          </select>
        </label>

        <label>
          Vehicle:{" "}
          <select
            value={vehicleFilter}
            onChange={(e) => {
              setPage(1);
              setVehicleFilter(e.target.value as VehicleFilter);
            }}
          >
            <option value="all">All</option>
            <option value="assigned">Assigned</option>
            <option value="unassigned">Not Assigned</option>
          </select>
        </label>
      </div>

      {error && <p role="alert">{error}</p>}

      {isLoading ? (
        <div className="stack">
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
        </div>
      ) : drivers.length === 0 ? (
        <div className="empty-state">
          <p className="empty-state__title">No Drivers Found</p>
          <p>Try adjusting your filters.</p>
        </div>
      ) : (
        <div className="table-responsive" style={{ overflowX: "auto" }}>
        <table>
          <thead>
            <tr>
              <th>Driver</th>
              <th>Email</th>
              <th>Phone</th>
              <th>Availability</th>
              <th>Vehicle</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {drivers.map((driver) => (
              <tr key={driver.id}>
                <td data-label="Driver">
                  {driver.firstName} {driver.lastName}
                </td>
                <td data-label="Email">{driver.email}</td>
                <td data-label="Phone">{driver.phone}</td>
                <td data-label="Availability">
                  <StatusBadge status={driver.isAvailable ? "Available" : "Unavailable"} />
                </td>
                <td data-label="Vehicle">{driver.vehicle ? `${driver.vehicle.make} ${driver.vehicle.model}` : "Not assigned"}</td>
                <td data-label="Status">
                  <StatusBadge status={driver.isActive ? "Active" : "Inactive"} />
                </td>
                <td data-label="Actions">
                  <div className="row" style={{ gap: "var(--space-2)" }}>
                    <Link to={`/admin/drivers/${driver.id}`}>View Details</Link>
                    <button type="button" className="btn-ghost" onClick={() => setModalState({ driver })}>
                      Edit
                    </button>
                    <button type="button" className="btn-ghost" onClick={() => handleToggleActive(driver)}>
                      {driver.isActive ? "Deactivate" : "Activate"}
                    </button>
                    <button type="button" className="btn-ghost" onClick={() => setPasswordResetDriver(driver)}>
                      Reset Password
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
        <DriverFormModal
          driver={modalState.driver}
          activeVehicles={activeVehicles}
          onSave={handleSave}
          onClose={() => setModalState(null)}
        />
      )}

      {passwordResetDriver && (
        <ResetPasswordModal
          driver={passwordResetDriver}
          onSave={handleResetPassword}
          onClose={() => setPasswordResetDriver(null)}
        />
      )}

      {deactivateTarget && (
        <ConfirmDialog
          title="Deactivate Driver"
          message={`Deactivate ${deactivateTarget.firstName} ${deactivateTarget.lastName}? They will no longer be able to log in or be assigned new bookings.`}
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

export default DriversPage;
