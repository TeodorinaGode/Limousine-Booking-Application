import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../../context/AuthContext";
import { useToast } from "../../context/ToastContext";
import {
  createAvailability,
  deleteAvailability,
  getMySchedule,
  setCurrentAvailability,
  updateAvailability,
} from "../../services/availabilityService";
import ConfirmDialog from "../../components/ConfirmDialog";
import DriverNav from "../../components/DriverNav";
import PageHeader from "../../components/PageHeader";
import StatusBadge from "../../components/StatusBadge";
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
  const { showToast } = useToast();

  const [isCurrentlyAvailable, setIsCurrentlyAvailable] = useState(false);
  const [schedule, setSchedule] = useState<AvailabilityDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isTogglingAvailability, setIsTogglingAvailability] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [modalState, setModalState] = useState<{ availability?: AvailabilityDto } | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<AvailabilityDto | null>(null);

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

  const handleToggleCurrentAvailability = async () => {
    if (!accessToken) return;

    setIsTogglingAvailability(true);
    setError(null);
    try {
      const result = await setCurrentAvailability(!isCurrentlyAvailable, accessToken);
      setIsCurrentlyAvailable(result.isAvailable);
      showToast(`Current availability set to ${result.isAvailable ? "Available" : "Unavailable"}.`);
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
      showToast("Availability updated successfully.");
    } else {
      await createAvailability(payload, accessToken);
      showToast("Availability created successfully.");
    }

    setModalState(null);
    await loadSchedule();
  };

  const handleConfirmDelete = async () => {
    if (!accessToken || !deleteTarget || deletingId) return;

    setDeletingId(deleteTarget.id);
    try {
      await deleteAvailability(deleteTarget.id, accessToken);
      showToast("Availability removed.");
      await loadSchedule();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to remove the availability period.");
    } finally {
      setDeletingId(null);
      setDeleteTarget(null);
    }
  };

  return (
    <div className="app-shell">
      <DriverNav />
      <main className="app-main app-main--narrow">
      <PageHeader title="My Availability" />

      <section className="card" style={{ marginBottom: "1.5rem" }}>
        <h2>Current availability</h2>
        <p>
          <StatusBadge status={isCurrentlyAvailable ? "Available" : "Unavailable"} />
        </p>
        <button type="button" onClick={handleToggleCurrentAvailability} disabled={isTogglingAvailability || isLoading}>
          {isTogglingAvailability ? "Updating..." : isCurrentlyAvailable ? "Set Unavailable" : "Set Available"}
        </button>
      </section>

      {error && <p role="alert">{error}</p>}

      <section>
        <div className="row row--between" style={{ marginBottom: "1rem" }}>
          <h2 style={{ margin: 0 }}>Schedule</h2>
          <button type="button" onClick={() => setModalState({})}>
            Add Availability
          </button>
        </div>

        {isLoading ? (
          <div className="skeleton skeleton-line" style={{ height: 40 }} />
        ) : schedule.length === 0 ? (
          <div className="empty-state">
            <p className="empty-state__title">No Availability Periods Yet</p>
            <p>Add a period to start managing your schedule.</p>
          </div>
        ) : (
          <div className="table-responsive" style={{ overflowX: "auto" }}>
          <table>
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
                  <td data-label="Date">{formatDate(item.date)}</td>
                  <td data-label="Start">{formatTime(item.startTime)}</td>
                  <td data-label="End">{formatTime(item.endTime)}</td>
                  <td data-label="Status">
                    <StatusBadge status={item.isAvailable ? "Available" : "Unavailable"} />
                  </td>
                  <td data-label="Notes">{item.notes}</td>
                  <td data-label="Actions">
                    <div className="row" style={{ gap: "var(--space-2)" }}>
                      <button
                        type="button"
                        className="btn-ghost"
                        onClick={() => setModalState({ availability: item })}
                        disabled={deletingId === item.id}
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        className="btn-ghost"
                        onClick={() => setDeleteTarget(item)}
                        disabled={deletingId === item.id}
                      >
                        {deletingId === item.id ? "Removing..." : "Remove"}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        )}
      </section>

      {modalState && (
        <AvailabilityFormModal
          availability={modalState.availability}
          onSave={handleSave}
          onClose={() => setModalState(null)}
        />
      )}

      {deleteTarget && (
        <ConfirmDialog
          title="Remove Availability Period"
          message={`Remove the ${formatDate(deleteTarget.date)} ${formatTime(deleteTarget.startTime)}–${formatTime(deleteTarget.endTime)} period from your schedule?`}
          confirmLabel="Remove"
          danger
          isBusy={deletingId === deleteTarget.id}
          onConfirm={handleConfirmDelete}
          onCancel={() => setDeleteTarget(null)}
        />
      )}
      </main>
    </div>
  );
}

export default AvailabilityPage;
