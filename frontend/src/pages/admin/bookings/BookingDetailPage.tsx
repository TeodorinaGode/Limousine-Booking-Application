import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "../../../context/AuthContext";
import { useToast } from "../../../context/ToastContext";
import { assignDriver, autoAssign, cancelBooking, getBookingById, refundPayment, resendConfirmation, updateBooking } from "../../../services/adminBookingService";
import { getDrivers } from "../../../services/driverService";
import { getRoutes } from "../../../services/routeService";
import AdminNav from "../../../components/AdminNav";
import ConfirmDialog from "../../../components/ConfirmDialog";
import StatusBadge from "../../../components/StatusBadge";
import type { AdminBookingDetailDto, AssignDriverRequest, UpdateBookingRequest } from "../../../types/adminBooking";
import type { DriverDto } from "../../../types/driver";
import type { RouteDto } from "../../../types/route";
import EditBookingModal from "./EditBookingModal";
import AssignBookingModal from "./AssignBookingModal";

function formatDate(dateIso: string): string {
  return new Date(`${dateIso}T00:00:00`).toLocaleDateString(undefined, { day: "2-digit", month: "long", year: "numeric" });
}

function formatTime(time: string): string {
  return time.slice(0, 5);
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString();
}

function BookingDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { accessToken } = useAuth();
  const { showToast } = useToast();

  const [booking, setBooking] = useState<AdminBookingDetailDto | null>(null);
  const [drivers, setDrivers] = useState<DriverDto[]>([]);
  const [routes, setRoutes] = useState<RouteDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isEditOpen, setIsEditOpen] = useState(false);
  const [isAssignOpen, setIsAssignOpen] = useState(false);
  const [busyAction, setBusyAction] = useState<"autoAssign" | "resend" | "refund" | "cancel" | null>(null);
  const [confirmDialog, setConfirmDialog] = useState<"refund" | "cancel" | null>(null);
  const isBusy = busyAction !== null;

  const loadBooking = useCallback(async () => {
    if (!accessToken || !id) return;
    setIsLoading(true);
    setError(null);
    try {
      setBooking(await getBookingById(id, accessToken));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load the booking.");
    } finally {
      setIsLoading(false);
    }
  }, [accessToken, id]);

  useEffect(() => {
    loadBooking();
  }, [loadBooking]);

  useEffect(() => {
    if (!accessToken) return;
    (async () => {
      try {
        const [driverResult, routeResult] = await Promise.all([
          getDrivers({ isActive: true, pageSize: 100 }, accessToken),
          getRoutes({ isActive: true, pageSize: 100 }, accessToken),
        ]);
        setDrivers(driverResult.items);
        setRoutes(routeResult.items);
      } catch {
        // Modal option lists are a convenience; failing to load them shouldn't block the page.
      }
    })();
  }, [accessToken]);

  const handleSaveEdit = async (values: UpdateBookingRequest) => {
    if (!accessToken || !id) return;
    setBooking(await updateBooking(id, values, accessToken));
    setIsEditOpen(false);
    showToast("Booking updated successfully.");
  };

  const handleAssign = async (values: AssignDriverRequest) => {
    if (!accessToken || !id) return;
    setBooking(await assignDriver(id, values, accessToken));
    setIsAssignOpen(false);
    showToast("Driver assigned successfully.");
  };

  const handleAutoAssign = async () => {
    if (!accessToken || !id) return;
    setError(null);
    setBusyAction("autoAssign");
    try {
      setBooking(await autoAssign(id, accessToken));
      showToast("Automatic assignment completed.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to run automatic assignment.");
    } finally {
      setBusyAction(null);
    }
  };

  const handleResendConfirmation = async () => {
    if (!accessToken || !id) return;
    setError(null);
    setBusyAction("resend");
    try {
      await resendConfirmation(id, accessToken);
      showToast("Confirmation email queued for resend.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to resend the confirmation email.");
    } finally {
      setBusyAction(null);
    }
  };

  const handleRefund = async () => {
    if (!accessToken || !id) return;

    setError(null);
    setBusyAction("refund");
    try {
      setBooking(await refundPayment(id, accessToken));
      showToast("Payment refunded.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to refund the payment.");
    } finally {
      setBusyAction(null);
      setConfirmDialog(null);
    }
  };

  const handleCancel = async (reason?: string) => {
    if (!accessToken || !id) return;

    setError(null);
    setBusyAction("cancel");
    try {
      setBooking(await cancelBooking(id, { reason }, accessToken));
      showToast("Booking cancelled.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to cancel the booking.");
    } finally {
      setBusyAction(null);
      setConfirmDialog(null);
    }
  };

  if (error && !booking)
    return (
      <div className="app-shell">
        <AdminNav />
        <main className="app-main">
          <p role="alert">{error}</p>
        </main>
      </div>
    );
  if (isLoading || !booking)
    return (
      <div className="app-shell">
        <AdminNav />
        <main className="app-main">
          <div className="skeleton skeleton-line" style={{ height: 40, maxWidth: 300 }} />
        </main>
      </div>
    );

  const canEdit = booking.status !== "Cancelled" && booking.status !== "Completed";
  const canCancel = booking.status !== "Cancelled" && booking.status !== "Completed";

  return (
    <div className="app-shell">
      <AdminNav />
      <main className="app-main">
      <p>
        <Link to="/admin/bookings">&larr; Back to Bookings</Link>
      </p>

      {error && <p role="alert">{error}</p>}

      <section style={{ marginBottom: "1.5rem" }}>
        <h1>{booking.bookingReference}</h1>
        <div className="row">
          <StatusBadge status={booking.status} />
          <StatusBadge status={booking.rideStatus} />
        </div>
        {booking.requiresManualAssignment && (
          <p role="alert" style={{ marginTop: "var(--space-4)" }}>Requires manual assignment: {booking.manualAssignmentReason}</p>
        )}
        {booking.status === "Cancelled" && (
          <p>
            Cancelled {booking.cancelledAt && `on ${formatDateTime(booking.cancelledAt)}`}
            {booking.cancelledByEmail && ` by ${booking.cancelledByEmail}`}
            {booking.cancellationReason && ` — ${booking.cancellationReason}`}
          </p>
        )}
      </section>

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Customer</h2>
        <p>
          {booking.customerFirstName} {booking.customerLastName}
          <br />
          {booking.customerEmail}
          <br />
          {booking.customerPhone}
        </p>
      </section>

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Trip</h2>
        <p>
          {booking.route.departureLocation} &rarr; {booking.route.destination}
        </p>
        <p>Date: {formatDate(booking.bookingDate)}</p>
        <p>Pickup: {formatTime(booking.pickupTime)}</p>
        <p>Estimated arrival: {formatTime(booking.estimatedEndTime)}</p>
      </section>

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Pickup</h2>
        <p>{booking.pickupAddress}</p>
      </section>

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Passengers</h2>
        <p>{booking.passengerCount}</p>
      </section>

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Price</h2>
        <p>
          {booking.currency} {booking.price.toFixed(2)}
        </p>
      </section>

      {booking.notes && (
        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Notes</h2>
          <p>{booking.notes}</p>
        </section>
      )}

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Assignment</h2>
        <p>Driver: {booking.driverName ?? "Not assigned"}</p>
        <p>Vehicle: {booking.vehicleDescription ?? "Not assigned"}</p>
        <p>Assignment: {booking.assignmentType ?? "Unassigned"}</p>
      </section>

      <section style={{ marginBottom: "1.5rem" }}>
        <h2>Payment</h2>
        {booking.payment ? (
          <>
            <div className="row">
              <StatusBadge status={booking.payment.status} />
              <span>
                {booking.payment.amount.toFixed(2)} {booking.payment.currency}
              </span>
              <span>{booking.payment.provider}</span>
              {booking.payment.paidAt && <span>Paid {formatDateTime(booking.payment.paidAt)}</span>}
            </div>
            {booking.payment.status === "Paid" && (
              <button
                type="button"
                className="btn-danger"
                style={{ marginTop: "var(--space-3)" }}
                onClick={() => setConfirmDialog("refund")}
                disabled={isBusy}
              >
                Refund Payment
              </button>
            )}
          </>
        ) : (
          <p>No payment has been started for this booking.</p>
        )}

        {booking.paymentHistory.length > 0 && (
          <div className="table-responsive" style={{ marginTop: "var(--space-4)" }}>
          <table>
            <thead>
              <tr>
                <th>Status</th>
                <th>Amount</th>
                <th>Created</th>
                <th>Paid At</th>
                <th>Failure Reason</th>
              </tr>
            </thead>
            <tbody>
              {booking.paymentHistory.map((entry, index) => (
                <tr key={index}>
                  <td data-label="Status">
                    <StatusBadge status={entry.status} />
                  </td>
                  <td data-label="Amount">
                    {entry.amount.toFixed(2)} {entry.currency}
                  </td>
                  <td data-label="Created">{formatDateTime(entry.createdAt)}</td>
                  <td data-label="Paid At">{entry.paidAt ? formatDateTime(entry.paidAt) : "—"}</td>
                  <td data-label="Failure Reason">{entry.failureReason ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        )}
      </section>

      {booking.assignmentHistory.length > 0 && (
        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Assignment History</h2>
          <div className="table-responsive">
          <table>
            <thead>
              <tr>
                <th>Driver</th>
                <th>Vehicle</th>
                <th>Type</th>
                <th>Assigned By</th>
                <th>Assigned At</th>
              </tr>
            </thead>
            <tbody>
              {booking.assignmentHistory.map((entry, index) => (
                <tr key={index}>
                  <td data-label="Driver">{entry.driverName}</td>
                  <td data-label="Vehicle">{entry.vehicleDescription}</td>
                  <td data-label="Type">{entry.assignmentType}</td>
                  <td data-label="Assigned By">{entry.assignedByEmail ?? "System (automatic)"}</td>
                  <td data-label="Assigned At">{formatDateTime(entry.assignedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        </section>
      )}

      {booking.rideStatusHistory.length > 0 && (
        <section style={{ marginBottom: "1.5rem" }}>
          <h2>Ride Status History</h2>
          <div className="table-responsive">
          <table>
            <thead>
              <tr>
                <th>From</th>
                <th>To</th>
                <th>Changed At</th>
              </tr>
            </thead>
            <tbody>
              {booking.rideStatusHistory.map((entry, index) => (
                <tr key={index}>
                  <td data-label="From">{entry.previousStatus}</td>
                  <td data-label="To">{entry.newStatus}</td>
                  <td data-label="Changed At">{formatDateTime(entry.changedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          </div>
        </section>
      )}

      <div className="row">
        {canEdit && (
          <button type="button" className="btn-secondary" onClick={() => setIsEditOpen(true)} disabled={isBusy}>
            Edit
          </button>
        )}
        {canEdit && (
          <button type="button" className="btn-secondary" onClick={() => setIsAssignOpen(true)} disabled={isBusy}>
            {booking.driverId ? "Reassign Driver" : "Assign Driver"}
          </button>
        )}
        {canEdit && (
          <button type="button" className="btn-secondary" onClick={handleAutoAssign} disabled={isBusy}>
            {busyAction === "autoAssign" ? "Assigning..." : "Run Automatic Assignment"}
          </button>
        )}
        {booking.status === "Confirmed" && (
          <button type="button" className="btn-secondary" onClick={handleResendConfirmation} disabled={isBusy}>
            {busyAction === "resend" ? "Sending..." : "Resend Confirmation Email"}
          </button>
        )}
        {canCancel && (
          <button type="button" className="btn-danger" onClick={() => setConfirmDialog("cancel")} disabled={isBusy}>
            Cancel Booking
          </button>
        )}
      </div>

      {isEditOpen && <EditBookingModal booking={booking} routes={routes} onSave={handleSaveEdit} onClose={() => setIsEditOpen(false)} />}
      {isAssignOpen && (
        <AssignBookingModal booking={booking} drivers={drivers} onAssign={handleAssign} onClose={() => setIsAssignOpen(false)} />
      )}
      {confirmDialog === "refund" && (
        <ConfirmDialog
          title="Refund Payment"
          message="Refund this payment via the payment provider? This cannot be undone."
          confirmLabel="Refund Payment"
          danger
          isBusy={busyAction === "refund"}
          onConfirm={handleRefund}
          onCancel={() => setConfirmDialog(null)}
        />
      )}
      {confirmDialog === "cancel" && (
        <ConfirmDialog
          title="Cancel Booking"
          message="Are you sure you want to cancel this booking? The customer will be notified."
          confirmLabel="Cancel Booking"
          cancelLabel="Keep Booking"
          reasonLabel="Cancellation reason (optional)"
          danger
          isBusy={busyAction === "cancel"}
          onConfirm={handleCancel}
          onCancel={() => setConfirmDialog(null)}
        />
      )}
      </main>
    </div>
  );
}

export default BookingDetailPage;
