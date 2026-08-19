import { useState, type FormEvent } from "react";
import Modal from "../../../components/Modal";
import type { DriverDto } from "../../../types/driver";
import type { VehicleDto } from "../../../types/vehicle";

export interface DriverFormValues {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  password: string;
  isActive: boolean;
  vehicleId: string;
}

interface DriverFormModalProps {
  driver?: DriverDto;
  activeVehicles: VehicleDto[];
  onSave: (values: DriverFormValues) => Promise<void>;
  onClose: () => void;
}

function toFormValues(driver?: DriverDto): DriverFormValues {
  return {
    firstName: driver?.firstName ?? "",
    lastName: driver?.lastName ?? "",
    email: driver?.email ?? "",
    phone: driver?.phone ?? "",
    password: "",
    isActive: driver?.isActive ?? true,
    vehicleId: driver?.vehicle?.id ?? "",
  };
}

function validate(values: DriverFormValues, isEdit: boolean): Record<string, string> {
  const errors: Record<string, string> = {};

  if (!values.firstName.trim()) errors.firstName = "First name is required.";
  if (!values.lastName.trim()) errors.lastName = "Last name is required.";
  if (!values.email.trim()) errors.email = "Email is required.";
  else if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(values.email.trim())) errors.email = "Enter a valid email address.";
  if (!values.phone.trim()) errors.phone = "Phone is required.";
  if (!isEdit && values.password.length < 8) errors.password = "Password must be at least 8 characters.";

  return errors;
}

function DriverFormModal({ driver, activeVehicles, onSave, onClose }: DriverFormModalProps) {
  const [values, setValues] = useState<DriverFormValues>(toFormValues(driver));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const isEdit = Boolean(driver);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationErrors = validate(values, isEdit);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSubmitError(null);
    setIsSaving(true);
    try {
      await onSave(values);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : "Failed to save the driver.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title={isEdit ? "Edit Driver" : "Add Driver"} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="firstName">First name</label>
          <br />
          <input
            id="firstName"
            type="text"
            value={values.firstName}
            onChange={(e) => setValues({ ...values, firstName: e.target.value })}
          />
          {errors.firstName && <p role="alert">{errors.firstName}</p>}
        </div>

        <div>
          <label htmlFor="lastName">Last name</label>
          <br />
          <input
            id="lastName"
            type="text"
            value={values.lastName}
            onChange={(e) => setValues({ ...values, lastName: e.target.value })}
          />
          {errors.lastName && <p role="alert">{errors.lastName}</p>}
        </div>

        <div>
          <label htmlFor="email">Email</label>
          <br />
          <input id="email" type="email" value={values.email} onChange={(e) => setValues({ ...values, email: e.target.value })} />
          {errors.email && <p role="alert">{errors.email}</p>}
        </div>

        <div>
          <label htmlFor="phone">Phone</label>
          <br />
          <input id="phone" type="text" value={values.phone} onChange={(e) => setValues({ ...values, phone: e.target.value })} />
          {errors.phone && <p role="alert">{errors.phone}</p>}
        </div>

        {!isEdit && (
          <div>
            <label htmlFor="password">Password</label>
            <br />
            <input
              id="password"
              type="password"
              value={values.password}
              onChange={(e) => setValues({ ...values, password: e.target.value })}
            />
            {errors.password && <p role="alert">{errors.password}</p>}
          </div>
        )}

        <div>
          <label htmlFor="vehicleId">Vehicle</label>
          <br />
          <select id="vehicleId" value={values.vehicleId} onChange={(e) => setValues({ ...values, vehicleId: e.target.value })}>
            <option value="">No vehicle</option>
            {activeVehicles.map((vehicle) => (
              <option key={vehicle.id} value={vehicle.id}>
                {vehicle.make} {vehicle.model} - {vehicle.registrationNumber}
              </option>
            ))}
          </select>
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

export default DriverFormModal;
