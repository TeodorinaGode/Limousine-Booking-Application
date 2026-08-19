import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import {
  createAvailability,
  deleteAvailability,
  getMySchedule,
  setCurrentAvailability,
  updateAvailability,
} from "../../services/availabilityService";
import type { AvailabilityDto } from "../../types/availability";
import AvailabilityFormModal, { type AvailabilityFormValues } from "./AvailabilityFormModal";

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function formatTime(time: string): string {
  return time.slice(0, 5);
}

function AvailabilityPage() {
  const { accessToken } = useAuth();

  const [isCurrentlyAvailable, setIsCurrentlyAvailable] = useState(false);
  const [schedule, setSchedule] = useState<AvailabilityDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isTogglingAvailability, setIsTogglingAvailability] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [modalState, setModalState] = useState<{ availability?: AvailabilityDto } | null>(null);

  const loadSchedule = useCallback(async () => {
    if (!accessToken) return;

    setIsLoading(true);
    setError(null);
    try {
      const result = await getMySchedule({}, accessToken);
      setIsCurrentlyAvailable(result.isCurrentlyAvailable);
      setSchedule(result.schedule);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load your availability.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken]);

  useEffect(() => {
    loadSchedule();
  }, [loadSchedule]);

  useEffect(() => {
    if (!successMessage) return;
    const timeout = setTimeout(() => setSuccessMessage(null), 3000);
    return () => clearTimeout(timeout);
  }, [successMessage]);

  const handleToggleCurrentAvailability = async () => {
    if (!accessToken) return;

    setIsTogglingAvailability(true);
    setError(null);
    try {
      const result = await setCurrentAvailability(!isCurrentlyAvailable, accessToken);
      setIsCurrentlyAvailable(result.isAvailable);
      setSuccessMessage(`Current availability set to ${result.isAvailable ? "Available" : "Unavailable"}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update your current availability.");
    } finally {
      setIsTogglingAvailability(false);
    }
  };

  const handleSave = async (values: AvailabilityFormValues) => {
    if (!accessToken) return;

    const payload = {
      date: values.date,
      startTime: values.startTime,
      endTime: values.endTime,
      isAvailable: values.isAvailable,
      notes: values.notes.trim() === "" ? null : values.notes,
    };

    if (modalState?.availability) {
      await updateAvailability(modalState.availability.id, payload, accessToken);
      setSuccessMessage("Availability updated successfully.");
    } else {
      await createAvailability(payload, accessToken);
      setSuccessMessage("Availability created successfully.");
    }

    setModalState(null);
    await loadSchedule();
  };

  const handleDelete = async (availability: AvailabilityDto) => {
    if (!accessToken) return;

    const confirmed = window.confirm("Are you sure you want to remove this availability period?");
    if (!confirmed) return;

    try {
      await deleteAvailability(availability.id, accessToken);
      setSuccessMessage("Availability removed.");
      await loadSchedule();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to remove the availability period.");
    }
  };

  return (
    <div>
      <h1>My Availability</h1>

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Current availability</h2>
        <p>{isCurrentlyAvailable ? "Available" : "Unavailable"}</p>
        <button type="button" onClick={handleToggleCurrentAvailability} disabled={isTogglingAvailability || isLoading}>
          {isTogglingAvailability ? "Updating..." : isCurrentlyAvailable ? "Set Unavailable" : "Set Available"}
        </button>
      </section>

      {successMessage && <p role="status">{successMessage}</p>}
      {error && <p role="alert">{error}</p>}

      <section>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "1rem" }}>
          <h2 style={{ margin: 0 }}>Schedule</h2>
          <button type="button" onClick={() => setModalState({})}>
            Add Availability
          </button>
        </div>

        {isLoading ? (
          <p>Loading schedule...</p>
        ) : schedule.length === 0 ? (
          <p>No availability periods yet.</p>
        ) : (
          <table style={{ width: "100%", borderCollapse: "collapse" }}>
            <thead>
              <tr>
                <th>Date</th>
                <th>Start</th>
                <th>End</th>
                <th>Status</th>
                <th>Notes</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {schedule.map((item) => (
                <tr key={item.id}>
                  <td>{formatDate(item.date)}</td>
                  <td>{formatTime(item.startTime)}</td>
                  <td>{formatTime(item.endTime)}</td>
                  <td>{item.isAvailable ? "Available" : "Unavailable"}</td>
                  <td>{item.notes}</td>
                  <td>
                    <button type="button" onClick={() => setModalState({ availability: item })}>
                      Edit
                    </button>{" "}
                    <button type="button" onClick={() => handleDelete(item)}>
                      Remove
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {modalState && (
        <AvailabilityFormModal
          availability={modalState.availability}
          onSave={handleSave}
          onClose={() => setModalState(null)}
        />
      )}
    </div>
  );
}

export default AvailabilityPage;
