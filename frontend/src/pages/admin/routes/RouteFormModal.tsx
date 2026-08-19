import { useState, type FormEvent } from "react";
import Modal from "../../../components/Modal";
import type { RouteDto } from "../../../types/route";

export interface RouteFormValues {
  departureLocation: string;
  destination: string;
  estimatedDurationMinutes: number;
  price: number;
  currency: string;
  isActive: boolean;
}

interface RouteFormModalProps {
  route?: RouteDto;
  onSave: (values: RouteFormValues) => Promise<void>;
  onClose: () => void;
}

function toFormValues(route?: RouteDto): RouteFormValues {
  return {
    departureLocation: route?.departureLocation ?? "",
    destination: route?.destination ?? "",
    estimatedDurationMinutes: route?.estimatedDurationMinutes ?? 0,
    price: route?.price ?? 0,
    currency: route?.currency ?? "CHF",
    isActive: route?.isActive ?? true,
  };
}

function validate(values: RouteFormValues): Record<string, string> {
  const errors: Record<string, string> = {};

  if (!values.departureLocation.trim()) errors.departureLocation = "Departure location is required.";
  if (!values.destination.trim()) errors.destination = "Destination is required.";
  if (!Number.isFinite(values.estimatedDurationMinutes) || values.estimatedDurationMinutes <= 0) {
    errors.estimatedDurationMinutes = "Estimated duration must be greater than zero.";
  }
  if (!Number.isFinite(values.price) || values.price < 0) errors.price = "Price must not be negative.";
  if (!values.currency.trim()) errors.currency = "Currency is required.";

  return errors;
}

function RouteFormModal({ route, onSave, onClose }: RouteFormModalProps) {
  const [values, setValues] = useState<RouteFormValues>(toFormValues(route));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isEdit = Boolean(route);

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
      setSubmitError(error instanceof Error ? error.message : "Failed to save the route.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? "Edit Route" : "Add Route"} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="departureLocation">Departure location</label>
          <br />
          <input
            id="departureLocation"
            type="text"
            value={values.departureLocation}
            onChange={(e) => setValues({ ...values, departureLocation: e.target.value })}
          />
          {errors.departureLocation && <p role="alert">{errors.departureLocation}</p>}
        </div>

        <div>
          <label htmlFor="destination">Destination</label>
          <br />
          <input
            id="destination"
            type="text"
            value={values.destination}
            onChange={(e) => setValues({ ...values, destination: e.target.value })}
          />
          {errors.destination && <p role="alert">{errors.destination}</p>}
        </div>

        <div>
          <label htmlFor="estimatedDurationMinutes">Estimated duration (minutes)</label>
          <br />
          <input
            id="estimatedDurationMinutes"
            type="number"
            value={values.estimatedDurationMinutes}
            onChange={(e) => setValues({ ...values, estimatedDurationMinutes: Number(e.target.value) })}
          />
          {errors.estimatedDurationMinutes && <p role="alert">{errors.estimatedDurationMinutes}</p>}
        </div>

        <div>
          <label htmlFor="price">Price</label>
          <br />
          <input
            id="price"
            type="number"
            step="0.01"
            value={values.price}
            onChange={(e) => setValues({ ...values, price: Number(e.target.value) })}
          />
          {errors.price && <p role="alert">{errors.price}</p>}
        </div>

        <div>
          <label htmlFor="currency">Currency</label>
          <br />
          <input
            id="currency"
            type="text"
            value={values.currency}
            onChange={(e) => setValues({ ...values, currency: e.target.value })}
          />
          {errors.currency && <p role="alert">{errors.currency}</p>}
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

export default RouteFormModal;
