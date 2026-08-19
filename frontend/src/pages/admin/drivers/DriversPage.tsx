import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../../../context/AuthContext";
import {
  activateDriver,
  createDriver,
  deactivateDriver,
  getDrivers,
  resetDriverPassword,
  updateDriver,
} from "../../../services/driverService";
import { getVehicles } from "../../../services/vehicleService";
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
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [modalState, setModalState] = useState<{ driver?: DriverDto } | null>(null);
  const [passwordResetDriver, setPasswordResetDriver] = useState<DriverDto | null>(null);
  const [activeVehicles, setActiveVehicles] = useState<VehicleDto[]>([]);

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

  useEffect(() => {
    if (!successMessage) return;
    const timeout = setTimeout(() => setSuccessMessage(null), 3000);
    return () => clearTimeout(timeout);
  }, [successMessage]);

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
      setSuccessMessage("Driver updated successfully.");
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
      setSuccessMessage("Driver created successfully.");
    }

    setModalState(null);
    await loadDrivers();
    await loadActiveVehicles();
  };

  const handleToggleActive = async (driver: DriverDto) => {
    if (!accessToken) return;

    if (driver.isActive) {
      const confirmed = window.confirm("Are you sure you want to deactivate this driver?");
      if (!confirmed) return;
    }

    try {
      if (driver.isActive) {
        await deactivateDriver(driver.id, accessToken);
        setSuccessMessage("Driver deactivated.");
      } else {
        await activateDriver(driver.id, accessToken);
        setSuccessMessage("Driver activated.");
      }
      await loadDrivers();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update the driver.");
    }
  };

  const handleResetPassword = async (newPassword: string) => {
    if (!accessToken || !passwordResetDriver) return;

    await resetDriverPassword(passwordResetDriver.id, { newPassword }, accessToken);
    setPasswordResetDriver(null);
    setSuccessMessage("Password reset successfully.");
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div>
      <h1>Driver Management</h1>

      <div style={{ display: "flex", gap: "1rem", alignItems: "center", marginBottom: "1rem", flexWrap: "wrap" }}>
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

        <button type="button" onClick={() => setModalState({})}>
          Add Driver
        </button>
      </div>

      {successMessage && <p role="status">{successMessage}</p>}
      {error && <p role="alert">{error}</p>}

      {isLoading ? (
        <p>Loading drivers...</p>
      ) : drivers.length === 0 ? (
        <p>No drivers found.</p>
      ) : (
        <table style={{ width: "100%", borderCollapse: "collapse" }}>
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
                <td>
                  {driver.firstName} {driver.lastName}
                </td>
                <td>{driver.email}</td>
                <td>{driver.phone}</td>
                <td>{driver.isAvailable ? "Available" : "Unavailable"}</td>
                <td>{driver.vehicle ? `${driver.vehicle.make} ${driver.vehicle.model}` : "Not assigned"}</td>
                <td>{driver.isActive ? "Active" : "Inactive"}</td>
                <td>
                  <button type="button" onClick={() => setModalState({ driver })}>
                    Edit
                  </button>{" "}
                  <button type="button" onClick={() => handleToggleActive(driver)}>
                    {driver.isActive ? "Deactivate" : "Activate"}
                  </button>{" "}
                  <button type="button" onClick={() => setPasswordResetDriver(driver)}>
                    Reset Password
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {totalPages > 1 && (
        <div style={{ marginTop: "1rem" }}>
          <button type="button" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
            Previous
          </button>{" "}
          <span>
            Page {page} of {totalPages}
          </span>{" "}
          <button type="button" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
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
    </div>
  );
}

export default DriversPage;
