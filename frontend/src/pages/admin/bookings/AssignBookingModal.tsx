import { useState } from "react";
import Modal from "../../../components/Modal";
import type { AdminBookingDetailDto, AssignDriverRequest } from "../../../types/adminBooking";
import type { DriverDto } from "../../../types/driver";

interface AssignBookingModalProps {
  booking: AdminBookingDetailDto;
  drivers: DriverDto[];
  onAssign: (values: AssignDriverRequest) => Promise<void>;
  onClose: () => void;
}

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, { day: "2-digit", month: "short", year: "numeric" });
}

function AssignBookingModal({ booking, drivers, onAssign, onClose }: AssignBookingModalProps) {
  const [driverId, setDriverId] = useState(booking.driverId ?? "");
  const [isSaving, setIsSaving] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  const selectedDriver = drivers.find((d) => d.id === driverId);

  const handleAssign = async () => {
    if (!selectedDriver?.vehicle) return;

    setSubmitError(null);
    setIsSaving(true);
    try {
      await onAssign({ driverId: selectedDriver.id, vehicleId: selectedDriver.vehicle.id });
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : "Failed to assign the driver.");
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal title="Assign Booking" onClose={onClose}>
      <p>
        Booking: {booking.bookingReference}
        <br />
        Date: {formatDate(booking.bookingDate)}
        <br />
        Time: {booking.pickupTime.slice(0, 5)}
        <br />
        Passengers: {booking.passengerCount}
      </p>

      <div>
        <label htmlFor="assign-driverId">Driver</label>
        <br />
        <select id="assign-driverId" value={driverId} onChange={(e) => setDriverId(e.target.value)}>
          <option value="">Select a driver...</option>
          {drivers.map((driver) => (
            <option key={driver.id} value={driver.id}>
              {driver.firstName} {driver.lastName} {driver.isAvailable ? "— Available" : "— Unavailable"}
            </option>
          ))}
        </select>
      </div>

      {selectedDriver && (
        <p role="status">
          Vehicle:{" "}
          {selectedDriver.vehicle
            ? `${selectedDriver.vehicle.make} ${selectedDriver.vehicle.model} - ${selectedDriver.vehicle.registrationNumber}`
            : "This driver has no vehicle assigned and cannot be selected."}
          <br />
          Current availability: {selectedDriver.isAvailable ? "Available" : "Unavailable"}
        </p>
      )}

      {submitError && <p role="alert">{submitError}</p>}

      <div style={{ marginTop: "1rem" }}>
        <button type="button" onClick={onClose} disabled={isSaving}>
          Cancel
        </button>{" "}
        <button type="button" onClick={handleAssign} disabled={isSaving || !selectedDriver?.vehicle}>
          {isSaving ? "Assigning..." : "Assign"}
        </button>
      </div>
    </Modal>
  );
}

export default AssignBookingModal;
