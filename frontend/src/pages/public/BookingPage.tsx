import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { createBooking, getActiveRoutes } from "../../services/bookingService";
import { ApiError } from "../../services/apiClient";
import { APP_BRAND_NAME } from "../../config/brand";
import type { BookingDto, PublicRouteDto } from "../../types/booking";

const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
const PHONE_PATTERN = /^[0-9+\-\s()]{7,25}$/;

interface FormValues {
  routeId: string;
  bookingDate: string;
  pickupTime: string;
  pickupAddress: string;
  passengerCount: number;
  customerFirstName: string;
  customerLastName: string;
  customerEmail: string;
  customerPhone: string;
  notes: string;
}

const initialValues: FormValues = {
  routeId: "",
  bookingDate: "",
  pickupTime: "",
  pickupAddress: "",
  passengerCount: 1,
  customerFirstName: "",
  customerLastName: "",
  customerEmail: "",
  customerPhone: "",
  notes: "",
};

type Step = 1 | 2 | 3 | 4;

const STEP_TITLES: Record<Step, string> = {
  1: "Trip",
  2: "Pickup Details",
  3: "Your Information",
  4: "Review & Confirm",
};

function validateStep1(values: FormValues): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!values.routeId) errors.routeId = "Please select a route.";
  if (!values.bookingDate) {
    errors.bookingDate = "Booking date is required.";
  } else if (values.bookingDate < new Date().toISOString().slice(0, 10)) {
    errors.bookingDate = "Booking date must be today or later.";
  }
  if (!values.pickupTime) errors.pickupTime = "Pickup time is required.";
  return errors;
}

function validateStep2(values: FormValues): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!values.pickupAddress.trim()) errors.pickupAddress = "Pickup address is required.";
  if (!Number.isFinite(values.passengerCount) || values.passengerCount < 1) {
    errors.passengerCount = "At least one passenger is required.";
  }
  return errors;
}

function validateStep3(values: FormValues): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!values.customerFirstName.trim()) errors.customerFirstName = "First name is required.";
  if (!values.customerLastName.trim()) errors.customerLastName = "Last name is required.";
  if (!values.customerEmail.trim()) {
    errors.customerEmail = "Email is required.";
  } else if (!EMAIL_PATTERN.test(values.customerEmail.trim())) {
    errors.customerEmail = "Enter a valid email address.";
  }
  if (!values.customerPhone.trim()) {
    errors.customerPhone = "Phone number is required.";
  } else if (!PHONE_PATTERN.test(values.customerPhone.trim())) {
    errors.customerPhone = "Enter a valid phone number.";
  }
  return errors;
}

function BookingPage() {
  const [routes, setRoutes] = useState<PublicRouteDto[]>([]);
  const [isLoadingRoutes, setIsLoadingRoutes] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [step, setStep] = useState<Step>(1);
  const [values, setValues] = useState<FormValues>(initialValues);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [confirmedBooking, setConfirmedBooking] = useState<BookingDto | null>(null);

  useEffect(() => {
    (async () => {
      setIsLoadingRoutes(true);
      setLoadError(null);
      try {
        setRoutes(await getActiveRoutes());
      } catch (err) {
        setLoadError(err instanceof Error ? err.message : "Failed to load routes.");
      } finally {
        setIsLoadingRoutes(false);
      }
    })();
  }, []);

  const selectedRoute = routes.find((r) => r.id === values.routeId);

  const goToNextStep = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const stepErrors =
      step === 1 ? validateStep1(values) : step === 2 ? validateStep2(values) : validateStep3(values);
    setErrors(stepErrors);
    if (Object.keys(stepErrors).length > 0) return;

    setStep((current) => (current < 4 ? ((current + 1) as Step) : current));
  };

  const goToPreviousStep = () => {
    setErrors({});
    setStep((current) => (current > 1 ? ((current - 1) as Step) : current));
  };

  const handleConfirm = async () => {
    setSubmitError(null);
    setIsSubmitting(true);
    try {
      const booking = await createBooking({
        routeId: values.routeId,
        bookingDate: values.bookingDate,
        pickupTime: values.pickupTime,
        pickupAddress: values.pickupAddress.trim(),
        passengerCount: values.passengerCount,
        customerFirstName: values.customerFirstName.trim(),
        customerLastName: values.customerLastName.trim(),
        customerEmail: values.customerEmail.trim(),
        customerPhone: values.customerPhone.trim(),
        notes: values.notes.trim() || undefined,
      });
      setConfirmedBooking(booking);
    } catch (err) {
      setSubmitError(
        err instanceof ApiError ? err.message : err instanceof Error ? err.message : "Failed to create the booking."
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  if (confirmedBooking) {
    const isConfirmed = confirmedBooking.status === "Confirmed";
    const statusMessage = isConfirmed
      ? "Your booking has been confirmed."
      : "Your booking request has been received and is awaiting confirmation.";

    return (
      <div className="container container--narrow fade-in" style={{ paddingTop: "var(--space-16)", paddingBottom: "var(--space-16)", textAlign: "center" }}>
        <div className="card">
          {isConfirmed && (
            <div aria-hidden="true" style={{ fontSize: "1.5rem", marginBottom: "var(--space-3)" }}>
              ✓
            </div>
          )}
          <p className="hero__eyebrow" style={{ marginBottom: "var(--space-4)" }}>
            {isConfirmed ? "Booking Confirmed" : "Booking Received"}
          </p>
          <h1 style={{ fontSize: "1.5rem", textTransform: "uppercase" }}>{confirmedBooking.bookingReference}</h1>
          <p role="status" style={{ display: "inline-block" }}>{statusMessage}</p>

          <dl style={{ textAlign: "left", marginTop: "var(--space-8)" }}>
            <dt>Trip</dt>
            <dd>
              {confirmedBooking.route.departureLocation} &rarr; {confirmedBooking.route.destination}
            </dd>

            <dt>Date &amp; time</dt>
            <dd>
              {confirmedBooking.bookingDate} at {confirmedBooking.pickupTime.slice(0, 5)}
            </dd>

            <dt>Pickup address</dt>
            <dd>{confirmedBooking.pickupAddress}</dd>

            <dt>Passengers</dt>
            <dd>{confirmedBooking.passengerCount}</dd>

            <dt>Price</dt>
            <dd>
              {confirmedBooking.price.toFixed(2)} {confirmedBooking.currency}
            </dd>
          </dl>

          <p style={{ marginTop: "var(--space-8)" }}>
            {isConfirmed
              ? "We look forward to driving you."
              : "A member of our team will contact you to confirm the details of your ride."}
          </p>
          <p className="text-muted" style={{ fontSize: "0.8rem" }}>A confirmation has been sent to your email.</p>
        </div>
        <p style={{ marginTop: "var(--space-6)" }}>
          <Link to="/">Return to home</Link>
        </p>
      </div>
    );
  }

  return (
    <div className="container container--medium" style={{ paddingTop: "var(--space-8)", paddingBottom: "var(--space-16)" }}>
      <p className="site-nav__brand" style={{ marginBottom: "var(--space-6)" }}>
        <Link to="/">{APP_BRAND_NAME}</Link>
      </p>
      <h1>Book Your Ride</h1>

      <div className="progress-steps" role="list" aria-label="Booking progress">
        {([1, 2, 3, 4] as const).map((s) => (
          <div key={s} className="progress-step" role="listitem">
            <span className={`progress-step__label${s === step ? " progress-step--active" : ""}${s < step ? " progress-step--done" : ""}`}>
              0{s} {STEP_TITLES[s]}
            </span>
            {s < 4 && <span className={`progress-step__line${s < step ? " progress-step--done" : ""}`} />}
          </div>
        ))}
      </div>

      {loadError && <p role="alert">{loadError}</p>}

      {isLoadingRoutes ? (
        <div className="stack">
          <div className="skeleton skeleton-line" style={{ height: 90 }} />
          <div className="skeleton skeleton-line" style={{ height: 90 }} />
        </div>
      ) : (
        <form onSubmit={step < 4 ? goToNextStep : (e) => e.preventDefault()} noValidate>
          {step === 1 && (
            <>
              <div className="form-group">
                <label>Route</label>
                {errors.routeId && <p className="form-error">{errors.routeId}</p>}
                <div className="stack" role="radiogroup" aria-label="Route">
                  {routes.map((route) => {
                    const isSelected = values.routeId === route.id;
                    return (
                      <button
                        type="button"
                        key={route.id}
                        role="radio"
                        aria-checked={isSelected}
                        className={`trip-card${isSelected ? " trip-card--selected" : ""}`}
                        style={{ textAlign: "left", background: isSelected ? "var(--color-surface-elevated)" : undefined, width: "100%" }}
                        onClick={() => setValues({ ...values, routeId: route.id })}
                      >
                        <div className="trip-card__route">
                          <span>{route.departureLocation}</span>
                          <span className="trip-card__arrow">&rarr;</span>
                          <span>{route.destination}</span>
                        </div>
                        <p className="trip-card__meta">Approx. {route.estimatedDurationMinutes} min</p>
                        <p className="trip-card__price">
                          {route.price.toFixed(2)} {route.currency}
                        </p>
                      </button>
                    );
                  })}
                </div>
              </div>

              <div className="row">
                <div className="form-group">
                  <label htmlFor="bookingDate">Booking date</label>
                  <br />
                  <input
                    id="bookingDate"
                    type="date"
                    value={values.bookingDate}
                    onChange={(e) => setValues({ ...values, bookingDate: e.target.value })}
                  />
                  {errors.bookingDate && <p className="form-error">{errors.bookingDate}</p>}
                </div>

                <div className="form-group">
                  <label htmlFor="pickupTime">Pickup time</label>
                  <br />
                  <input
                    id="pickupTime"
                    type="time"
                    value={values.pickupTime}
                    onChange={(e) => setValues({ ...values, pickupTime: e.target.value })}
                  />
                  {errors.pickupTime && <p className="form-error">{errors.pickupTime}</p>}
                </div>
              </div>
            </>
          )}

          {step === 2 && (
            <>
              <div className="form-group">
                <label htmlFor="pickupAddress">Pickup address</label>
                <br />
                <input
                  id="pickupAddress"
                  type="text"
                  placeholder="Enter pickup address"
                  value={values.pickupAddress}
                  onChange={(e) => setValues({ ...values, pickupAddress: e.target.value })}
                />
                {errors.pickupAddress && <p className="form-error">{errors.pickupAddress}</p>}
              </div>

              <div className="form-group">
                <label htmlFor="passengerCount">Number of passengers</label>
                <br />
                <input
                  id="passengerCount"
                  type="number"
                  min={1}
                  value={values.passengerCount}
                  onChange={(e) => setValues({ ...values, passengerCount: Number(e.target.value) })}
                />
                {errors.passengerCount && <p className="form-error">{errors.passengerCount}</p>}
              </div>

              <div className="form-group">
                <label htmlFor="notes">Notes (optional)</label>
                <br />
                <textarea
                  id="notes"
                  placeholder="Optional notes"
                  value={values.notes}
                  onChange={(e) => setValues({ ...values, notes: e.target.value })}
                />
              </div>
            </>
          )}

          {step === 3 && (
            <>
              <div className="row">
                <div className="form-group">
                  <label htmlFor="customerFirstName">First name</label>
                  <br />
                  <input
                    id="customerFirstName"
                    type="text"
                    value={values.customerFirstName}
                    onChange={(e) => setValues({ ...values, customerFirstName: e.target.value })}
                  />
                  {errors.customerFirstName && <p className="form-error">{errors.customerFirstName}</p>}
                </div>

                <div className="form-group">
                  <label htmlFor="customerLastName">Last name</label>
                  <br />
                  <input
                    id="customerLastName"
                    type="text"
                    value={values.customerLastName}
                    onChange={(e) => setValues({ ...values, customerLastName: e.target.value })}
                  />
                  {errors.customerLastName && <p className="form-error">{errors.customerLastName}</p>}
                </div>
              </div>

              <div className="form-group">
                <label htmlFor="customerEmail">Email</label>
                <br />
                <input
                  id="customerEmail"
                  type="email"
                  value={values.customerEmail}
                  onChange={(e) => setValues({ ...values, customerEmail: e.target.value })}
                />
                {errors.customerEmail && <p className="form-error">{errors.customerEmail}</p>}
              </div>

              <div className="form-group">
                <label htmlFor="customerPhone">Phone</label>
                <br />
                <input
                  id="customerPhone"
                  type="tel"
                  value={values.customerPhone}
                  onChange={(e) => setValues({ ...values, customerPhone: e.target.value })}
                />
                {errors.customerPhone && <p className="form-error">{errors.customerPhone}</p>}
              </div>
            </>
          )}

          {step === 4 && selectedRoute && (
            <dl className="card">
              <dt>Trip</dt>
              <dd>
                {selectedRoute.departureLocation} &rarr; {selectedRoute.destination}
              </dd>

              <dt>Date &amp; time</dt>
              <dd>
                {values.bookingDate} at {values.pickupTime}
              </dd>

              <dt>Pickup address</dt>
              <dd>{values.pickupAddress}</dd>

              <dt>Passengers</dt>
              <dd>{values.passengerCount}</dd>

              {values.notes && (
                <>
                  <dt>Notes</dt>
                  <dd>{values.notes}</dd>
                </>
              )}

              <dt>Name</dt>
              <dd>
                {values.customerFirstName} {values.customerLastName}
              </dd>

              <dt>Email</dt>
              <dd>{values.customerEmail}</dd>

              <dt>Phone</dt>
              <dd>{values.customerPhone}</dd>

              <dt>Estimated price</dt>
              <dd>
                {selectedRoute.price.toFixed(2)} {selectedRoute.currency}
              </dd>
            </dl>
          )}

          {submitError && <p role="alert">{submitError}</p>}

          <div className="row" style={{ marginTop: "var(--space-6)" }}>
            {step > 1 && (
              <button type="button" className="btn-secondary" onClick={goToPreviousStep} disabled={isSubmitting}>
                Back
              </button>
            )}
            {step < 4 ? (
              <button type="submit">Next</button>
            ) : (
              <button type="button" onClick={handleConfirm} disabled={isSubmitting}>
                {isSubmitting ? "Submitting..." : "Confirm Booking"}
              </button>
            )}
          </div>
        </form>
      )}
    </div>
  );
}

export default BookingPage;
