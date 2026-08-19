import { useState, type FormEvent } from "react";
import Modal from "../../../components/Modal";
import type { VehicleDto } from "../../../types/vehicle";

const VEHICLE_TYPE_SUGGESTIONS = ["Sedan", "SUV", "Van", "Limousine", "Minivan"];

export interface VehicleFormValues {
  registrationNumber: string;
  make: string;
  model: string;
  vehicleType: string;
  passengerCapacity: number;
  notes: string;
  isActive: boolean;
}

interface VehicleFormModalProps {
  vehicle?: VehicleDto;
  onSave: (values: VehicleFormValues) => Promise<void>;
  onClose: () => void;
}

function toFormValues(vehicle?: VehicleDto): VehicleFormValues {
  return {
    registrationNumber: vehicle?.registrationNumber ?? "",
    make: vehicle?.make ?? "",
    model: vehicle?.model ?? "",
    vehicleType: vehicle?.vehicleType ?? "",
    passengerCapacity: vehicle?.passengerCapacity ?? 1,
    notes: vehicle?.notes ?? "",
    isActive: vehicle?.isActive ?? true,
  };
}

function validate(values: VehicleFormValues): Record<string, string> {
  const errors: Record<string, string> = {};

  if (!values.registrationNumber.trim()) errors.registrationNumber = "Registration number is required.";
  if (!values.make.trim()) errors.make = "Make is required.";
  if (!values.model.trim()) errors.model = "Model is required.";
  if (!values.vehicleType.trim()) errors.vehicleType = "Vehicle type is required.";
  if (!Number.isFinite(values.passengerCapacity) || values.passengerCapacity <= 0) {
    errors.passengerCapacity = "Passenger capacity must be greater than zero.";
  }

  return errors;
}

function VehicleFormModal({ vehicle, onSave, onClose }: VehicleFormModalProps) {
  const [values, setValues] = useState<VehicleFormValues>(toFormValues(vehicle));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isEdit = Boolean(vehicle);

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
      setSubmitError(error instanceof Error ? error.message : "Failed to save the vehicle.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? "Edit Vehicle" : "Add Vehicle"} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="registrationNumber">Registration number</label>
          <br />
          <input
            id="registrationNumber"
            type="text"
            value={values.registrationNumber}
            onChange={(e) => setValues({ ...values, registrationNumber: e.target.value })}
          />
          {errors.registrationNumber && <p role="alert">{errors.registrationNumber}</p>}
        </div>

        <div>
          <label htmlFor="make">Make</label>
          <br />
          <input id="make" type="text" value={values.make} onChange={(e) => setValues({ ...values, make: e.target.value })} />
          {errors.make && <p role="alert">{errors.make}</p>}
        </div>

        <div>
          <label htmlFor="model">Model</label>
          <br />
          <input id="model" type="text" value={values.model} onChange={(e) => setValues({ ...values, model: e.target.value })} />
          {errors.model && <p role="alert">{errors.model}</p>}
        </div>

        <div>
          <label htmlFor="vehicleType">Vehicle type</label>
          <br />
          <input
            id="vehicleType"
            type="text"
            list="vehicleTypeSuggestions"
            value={values.vehicleType}
            onChange={(e) => setValues({ ...values, vehicleType: e.target.value })}
          />
          <datalist id="vehicleTypeSuggestions">
            {VEHICLE_TYPE_SUGGESTIONS.map((type) => (
              <option key={type} value={type} />
            ))}
          </datalist>
          {errors.vehicleType && <p role="alert">{errors.vehicleType}</p>}
        </div>

        <div>
          <label htmlFor="passengerCapacity">Passenger capacity</label>
          <br />
          <input
            id="passengerCapacity"
            type="number"
            value={values.passengerCapacity}
            onChange={(e) => setValues({ ...values, passengerCapacity: Number(e.target.value) })}
          />
          {errors.passengerCapacity && <p role="alert">{errors.passengerCapacity}</p>}
        </div>

        <div>
          <label htmlFor="notes">Notes</label>
          <br />
          <textarea id="notes" value={values.notes} onChange={(e) => setValues({ ...values, notes: e.target.value })} />
        </div>

        {isEdit && (
          <div>
            <label>
              <input
                type="checkbox"
                checked={values.isActive}
                onChange={(e) => setValues({ ...values, isActive: e.target.checked })}
              />
              {" "}Active
            </label>
          </div>
        )}

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

export default VehicleFormModal;
