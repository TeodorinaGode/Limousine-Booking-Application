import { useState, type FormEvent } from "react";
import Modal from "../../components/Modal";
import type { AvailabilityDto } from "../../types/availability";

export interface AvailabilityFormValues {
  date: string;
  startTime: string;
  endTime: string;
  isAvailable: boolean;
  notes: string;
}

interface AvailabilityFormModalProps {
  availability?: AvailabilityDto;
  onSave: (values: AvailabilityFormValues) => Promise<void>;
  onClose: () => void;
}

function toTimeInputValue(time?: string): string {
  // "08:00:00" -> "08:00" for an <input type="time">.
  return time ? time.slice(0, 5) : "";
}

function toFormValues(availability?: AvailabilityDto): AvailabilityFormValues {
  return {
    date: availability?.date ?? "",
    startTime: toTimeInputValue(availability?.startTime),
    endTime: toTimeInputValue(availability?.endTime),
    isAvailable: availability?.isAvailable ?? true,
    notes: availability?.notes ?? "",
  };
}

function validate(values: AvailabilityFormValues): Record<string, string> {
  const errors: Record<string, string> = {};

  if (!values.date) errors.date = "Date is required.";
  if (!values.startTime) errors.startTime = "Start time is required.";
  if (!values.endTime) errors.endTime = "End time is required.";
  if (values.startTime && values.endTime && values.endTime <= values.startTime) {
    errors.endTime = "End time must be after start time.";
  }

  return errors;
}

function AvailabilityFormModal({ availability, onSave, onClose }: AvailabilityFormModalProps) {
  const [values, setValues] = useState<AvailabilityFormValues>(toFormValues(availability));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isEdit = Boolean(availability);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationErrors = validate(values);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSubmitError(null);
    setIsSaving(true);
    try {
      await onSave(values);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : "Failed to save the availability period.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? "Edit Availability" : "Add Availability"} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="date">Date</label>
          <br />
          <input id="date" type="date" value={values.date} onChange={(e) => setValues({ ...values, date: e.target.value })} />
          {errors.date && <p role="alert">{errors.date}</p>}
        </div>

        <div>
          <label htmlFor="startTime">Start time</label>
          <br />
          <input
            id="startTime"
            type="time"
            value={values.startTime}
            onChange={(e) => setValues({ ...values, startTime: e.target.value })}
          />
          {errors.startTime && <p role="alert">{errors.startTime}</p>}
        </div>

        <div>
          <label htmlFor="endTime">End time</label>
          <br />
          <input
            id="endTime"
            type="time"
            value={values.endTime}
            onChange={(e) => setValues({ ...values, endTime: e.target.value })}
          />
          {errors.endTime && <p role="alert">{errors.endTime}</p>}
        </div>

        <div>
          <label htmlFor="status">Status</label>
          <br />
          <select
            id="status"
            value={values.isAvailable ? "available" : "unavailable"}
            onChange={(e) => setValues({ ...values, isAvailable: e.target.value === "available" })}
          >
            <option value="available">Available</option>
            <option value="unavailable">Unavailable</option>
          </select>
        </div>

        <div>
          <label htmlFor="notes">Notes</label>
          <br />
          <textarea id="notes" value={values.notes} onChange={(e) => setValues({ ...values, notes: e.target.value })} />
        </div>

        {submitError && <p role="alert">{submitError}</p>}

        <div style={{ marginTop: "1rem" }}>
          <button type="button" onClick={onClose} disabled={isSaving}>
            Cancel
          </button>{" "}
          <button type="submit" disabled={isSaving}>
            {isSaving ? "Saving..." : "Save"}
          </button>
        </div>
      </form>
    </Modal>
  );
}

export default AvailabilityFormModal;
