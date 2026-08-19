import { useState, type FormEvent } from "react";
import Modal from "../../../components/Modal";
import type { AdminBookingDetailDto, UpdateBookingRequest } from "../../../types/adminBooking";
import type { RouteDto } from "../../../types/route";

interface EditBookingModalProps {
  booking: AdminBookingDetailDto;
  routes: RouteDto[];
  onSave: (values: UpdateBookingRequest) => Promise<void>;
  onClose: () => void;
}

function toFormValues(booking: AdminBookingDetailDto): UpdateBookingRequest {
  return {
    routeId: booking.routeId,
    bookingDate: booking.bookingDate,
    pickupTime: booking.pickupTime.slice(0, 5),
    pickupAddress: booking.pickupAddress,
    passengerCount: booking.passengerCount,
    customerFirstName: booking.customerFirstName,
    customerLastName: booking.customerLastName,
    customerEmail: booking.customerEmail,
    customerPhone: booking.customerPhone,
    notes: booking.notes ?? "",
  };
}

const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

function validate(values: UpdateBookingRequest): Record<string, string> {
  const errors: Record<string, string> = {};

  if (!values.routeId) errors.routeId = "Route is required.";
  if (!values.bookingDate) errors.bookingDate = "Booking date is required.";
  if (!values.pickupTime) errors.pickupTime = "Pickup time is required.";
  if (!values.pickupAddress.trim()) errors.pickupAddress = "Pickup address is required.";
  if (!Number.isFinite(values.passengerCount) || values.passengerCount < 1) errors.passengerCount = "At least one passenger is required.";
  if (!values.customerFirstName.trim()) errors.customerFirstName = "First name is required.";
  if (!values.customerLastName.trim()) errors.customerLastName = "Last name is required.";
  if (!values.customerEmail.trim()) errors.customerEmail = "Email is required.";
  else if (!EMAIL_PATTERN.test(values.customerEmail.trim())) errors.customerEmail = "Enter a valid email address.";
  if (!values.customerPhone.trim()) errors.customerPhone = "Phone is required.";

  return errors;
}

function EditBookingModal({ booking, routes, onSave, onClose }: EditBookingModalProps) {
  const [values, setValues] = useState<UpdateBookingRequest>(toFormValues(booking));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const validationErrors = validate(values);
    setErrors(validationErrors);
    if (Object.keys(validationErrors).length > 0) return;

    setSubmitError(null);
    setIsSaving(true);
    try {
      await onSave({ ...values, notes: values.notes?.trim() || undefined });
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : "Failed to save the booking.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title={`Edit Booking ${booking.bookingReference}`} onClose={onClose}>
      <form onSubmit={handleSubmit} noValidate>
        <div>
          <label htmlFor="edit-routeId">Route</label>
          <br />
          <select id="edit-routeId" value={values.routeId} onChange={(e) => setValues({ ...values, routeId: e.target.value })}>
            <option value="">Select a route...</option>
            {routes.map((route) => (
              <option key={route.id} value={route.id}>
                {route.departureLocation} &rarr; {route.destination}
              </option>
            ))}
          </select>
          {errors.routeId && <p role="alert">{errors.routeId}</p>}
        </div>

        <div>
          <label htmlFor="edit-bookingDate">Booking date</label>
          <br />
          <input
            id="edit-bookingDate"
            type="date"
            value={values.bookingDate}
            onChange={(e) => setValues({ ...values, bookingDate: e.target.value })}
          />
          {errors.bookingDate && <p role="alert">{errors.bookingDate}</p>}
        </div>

        <div>
          <label htmlFor="edit-pickupTime">Pickup time</label>
          <br />
          <input
            id="edit-pickupTime"
            type="time"
            value={values.pickupTime}
            onChange={(e) => setValues({ ...values, pickupTime: e.target.value })}
          />
          {errors.pickupTime && <p role="alert">{errors.pickupTime}</p>}
        </div>

        <div>
          <label htmlFor="edit-pickupAddress">Pickup address</label>
          <br />
          <input
            id="edit-pickupAddress"
            type="text"
            value={values.pickupAddress}
            onChange={(e) => setValues({ ...values, pickupAddress: e.target.value })}
          />
          {errors.pickupAddress && <p role="alert">{errors.pickupAddress}</p>}
        </div>

        <div>
          <label htmlFor="edit-passengerCount">Passengers</label>
          <br />
          <input
            id="edit-passengerCount"
            type="number"
            min={1}
            value={values.passengerCount}
            onChange={(e) => setValues({ ...values, passengerCount: Number(e.target.value) })}
          />
          {errors.passengerCount && <p role="alert">{errors.passengerCount}</p>}
        </div>

        <div>
          <label htmlFor="edit-customerFirstName">First name</label>
          <br />
          <input
            id="edit-customerFirstName"
            type="text"
            value={values.customerFirstName}
            onChange={(e) => setValues({ ...values, customerFirstName: e.target.value })}
          />
          {errors.customerFirstName && <p role="alert">{errors.customerFirstName}</p>}
        </div>

        <div>
          <label htmlFor="edit-customerLastName">Last name</label>
          <br />
          <input
            id="edit-customerLastName"
            type="text"
            value={values.customerLastName}
            onChange={(e) => setValues({ ...values, customerLastName: e.target.value })}
          />
          {errors.customerLastName && <p role="alert">{errors.customerLastName}</p>}
        </div>

        <div>
          <label htmlFor="edit-customerEmail">Email</label>
          <br />
          <input
            id="edit-customerEmail"
            type="email"
            value={values.customerEmail}
            onChange={(e) => setValues({ ...values, customerEmail: e.target.value })}
          />
          {errors.customerEmail && <p role="alert">{errors.customerEmail}</p>}
        </div>

        <div>
          <label htmlFor="edit-customerPhone">Phone</label>
          <br />
          <input
            id="edit-customerPhone"
            type="text"
            value={values.customerPhone}
            onChange={(e) => setValues({ ...values, customerPhone: e.target.value })}
          />
          {errors.customerPhone && <p role="alert">{errors.customerPhone}</p>}
        </div>

        <div>
          <label htmlFor="edit-notes">Notes</label>
          <br />
          <textarea id="edit-notes" value={values.notes ?? ""} onChange={(e) => setValues({ ...values, notes: e.target.value })} />
        </div>

        {(values.routeId !== booking.routeId ||
          values.bookingDate !== booking.bookingDate ||
          values.pickupTime !== booking.pickupTime.slice(0, 5) ||
          values.passengerCount !== booking.passengerCount) && (
          <p role="status">Changing the route, date, time, or passenger count will re-check the current driver assignment.</p>
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

export default EditBookingModal;
