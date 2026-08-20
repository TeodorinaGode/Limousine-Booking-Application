import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { createBooking, getActiveRoutes } from "../../services/bookingService";
import { createPayment } from "../../services/paymentService";
import { ApiError } from "../../services/apiClient";
import { APP_BRAND_NAME } from "../../config/brand";
import LanguageSelector from "../../components/LanguageSelector";
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

function stepTitles(t: TFunction): Record<Step, string> {
  return {
    1: t("booking:steps.trip"),
    2: t("booking:steps.pickupDetails"),
    3: t("booking:steps.yourInformation"),
    4: t("booking:steps.reviewConfirm"),
  };
}

function validateStep1(values: FormValues, t: TFunction): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!values.routeId) errors.routeId = t("validation:selectRoute");
  if (!values.bookingDate) {
    errors.bookingDate = t("validation:bookingDateRequired");
  } else if (values.bookingDate < new Date().toISOString().slice(0, 10)) {
    errors.bookingDate = t("validation:bookingDateFuture");
  }
  if (!values.pickupTime) errors.pickupTime = t("validation:pickupTimeRequired");
  return errors;
}

function validateStep2(values: FormValues, t: TFunction): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!values.pickupAddress.trim()) errors.pickupAddress = t("validation:pickupAddressRequired");
  if (!Number.isFinite(values.passengerCount) || values.passengerCount < 1) {
    errors.passengerCount = t("validation:passengersRequired");
  }
  return errors;
}

function validateStep3(values: FormValues, t: TFunction): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!values.customerFirstName.trim()) errors.customerFirstName = t("validation:firstNameRequired");
  if (!values.customerLastName.trim()) errors.customerLastName = t("validation:lastNameRequired");
  if (!values.customerEmail.trim()) {
    errors.customerEmail = t("validation:emailRequired");
  } else if (!EMAIL_PATTERN.test(values.customerEmail.trim())) {
    errors.customerEmail = t("validation:emailInvalid");
  }
  if (!values.customerPhone.trim()) {
    errors.customerPhone = t("validation:phoneRequired");
  } else if (!PHONE_PATTERN.test(values.customerPhone.trim())) {
    errors.customerPhone = t("validation:phoneInvalid");
  }
  return errors;
}

function BookingPage() {
  const { t, i18n } = useTranslation(["booking", "common", "validation"]);

  const [routes, setRoutes] = useState<PublicRouteDto[]>([]);
  const [isLoadingRoutes, setIsLoadingRoutes] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [step, setStep] = useState<Step>(1);
  const [values, setValues] = useState<FormValues>(initialValues);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [confirmedBooking, setConfirmedBooking] = useState<BookingDto | null>(null);
  const [isStartingPayment, setIsStartingPayment] = useState(false);
  const [paymentError, setPaymentError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      setIsLoadingRoutes(true);
      setLoadError(null);
      try {
        setRoutes(await getActiveRoutes());
      } catch (err) {
        setLoadError(err instanceof Error ? err.message : t("common:misc.error"));
      } finally {
        setIsLoadingRoutes(false);
      }
    })();
  }, [t]);

  const selectedRoute = routes.find((r) => r.id === values.routeId);

  const goToNextStep = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const stepErrors =
      step === 1 ? validateStep1(values, t) : step === 2 ? validateStep2(values, t) : validateStep3(values, t);
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
        // Captured once, at booking creation, so the confirmation email uses the
        // language the customer was actually looking at — not whatever their
        // browser happens to report later when the outbox worker sends it.
        languageCode: i18n.resolvedLanguage,
      });
      setConfirmedBooking(booking);
    } catch (err) {
      setSubmitError(
        err instanceof ApiError ? err.message : err instanceof Error ? err.message : t("common:misc.error")
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const handlePayNow = async () => {
    if (!confirmedBooking || isStartingPayment) return;
    setPaymentError(null);
    setIsStartingPayment(true);
    try {
      const checkout = await createPayment(confirmedBooking.bookingReference, confirmedBooking.accessToken);
      window.location.href = checkout.checkoutUrl;
    } catch (err) {
      setPaymentError(err instanceof ApiError ? err.message : err instanceof Error ? err.message : t("common:misc.error"));
      setIsStartingPayment(false);
    }
  };

  if (confirmedBooking) {
    const isConfirmed = confirmedBooking.status === "Confirmed";
    const statusMessage = isConfirmed ? t("booking:success.confirmedMessage") : t("booking:success.pendingMessage");

    return (
      <div className="container container--narrow fade-in" style={{ paddingTop: "var(--space-16)", paddingBottom: "var(--space-16)", textAlign: "center" }}>
        <div className="card">
          {isConfirmed && (
            <div aria-hidden="true" style={{ fontSize: "1.5rem", marginBottom: "var(--space-3)" }}>
              ✓
            </div>
          )}
          <p className="hero__eyebrow" style={{ marginBottom: "var(--space-4)" }}>
            {isConfirmed ? t("booking:success.confirmedTitle") : t("booking:success.pendingTitle")}
          </p>
          <h1 style={{ fontSize: "1.5rem", textTransform: "uppercase" }}>{confirmedBooking.bookingReference}</h1>
          <p role="status" style={{ display: "inline-block" }}>{statusMessage}</p>

          <dl style={{ textAlign: "left", marginTop: "var(--space-8)" }}>
            <dt>{t("booking:success.trip")}</dt>
            <dd>
              {confirmedBooking.route.departureLocation} &rarr; {confirmedBooking.route.destination}
            </dd>

            <dt>{t("booking:success.dateTime")}</dt>
            <dd>
              {confirmedBooking.bookingDate} at {confirmedBooking.pickupTime.slice(0, 5)}
            </dd>

            <dt>{t("booking:success.pickupAddress")}</dt>
            <dd>{confirmedBooking.pickupAddress}</dd>

            <dt>{t("booking:success.passengers")}</dt>
            <dd>{confirmedBooking.passengerCount}</dd>

            <dt>{t("booking:success.price")}</dt>
            <dd>
              {confirmedBooking.price.toFixed(2)} {confirmedBooking.currency}
            </dd>
          </dl>

          <p style={{ marginTop: "var(--space-8)" }}>
            {isConfirmed ? t("booking:success.confirmedFooter") : t("booking:success.pendingFooter")}
          </p>
          <p className="text-muted" style={{ fontSize: "0.8rem" }}>{t("booking:success.emailSent")}</p>

          <div className="card" style={{ marginTop: "var(--space-8)", textAlign: "left" }}>
            <h2 style={{ marginTop: 0 }}>{t("booking:payment.title")}</h2>
            <p>
              {t("booking:payment.amountDue", { amount: `${confirmedBooking.price.toFixed(2)} ${confirmedBooking.currency}` })}
            </p>
            {paymentError && <p role="alert">{paymentError}</p>}
            <button type="button" onClick={handlePayNow} disabled={isStartingPayment}>
              {isStartingPayment ? t("booking:payment.redirecting") : t("booking:payment.payNow")}
            </button>
            <p className="text-muted" style={{ fontSize: "0.8rem", marginTop: "var(--space-3)" }}>
              {t("booking:payment.payLaterNotice")}{" "}
              <Link to={`/booking/payment/${confirmedBooking.bookingReference}?token=${confirmedBooking.accessToken}`}>{t("booking:payment.here")}</Link>.
            </p>
          </div>
        </div>
        <p style={{ marginTop: "var(--space-6)" }}>
          <Link to="/">{t("booking:success.returnHome")}</Link>
        </p>
      </div>
    );
  }

  const titles = stepTitles(t);

  return (
    <div className="container container--medium" style={{ paddingTop: "var(--space-8)", paddingBottom: "var(--space-16)" }}>
      <div className="row" style={{ justifyContent: "space-between", alignItems: "center", marginBottom: "var(--space-6)" }}>
        <p className="site-nav__brand" style={{ margin: 0 }}>
          <Link to="/">{APP_BRAND_NAME}</Link>
        </p>
        <LanguageSelector />
      </div>
      <h1>{t("booking:title")}</h1>

      <div className="progress-steps" role="list" aria-label="Booking progress">
        {([1, 2, 3, 4] as const).map((s) => (
          <div key={s} className="progress-step" role="listitem">
            <span className={`progress-step__label${s === step ? " progress-step--active" : ""}${s < step ? " progress-step--done" : ""}`}>
              0{s} {titles[s]}
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
                <label>{t("booking:selectRoute")}</label>
                {errors.routeId && <p className="form-error">{errors.routeId}</p>}
                <div className="stack" role="radiogroup" aria-label={t("booking:selectRoute")}>
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
                        <p className="trip-card__meta">{t("booking:approxMinutes", { count: route.estimatedDurationMinutes })}</p>
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
                  <label htmlFor="bookingDate">{t("booking:date")}</label>
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
                  <label htmlFor="pickupTime">{t("booking:time")}</label>
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
                <label htmlFor="pickupAddress">{t("booking:pickupAddress")}</label>
                <br />
                <input
                  id="pickupAddress"
                  type="text"
                  placeholder={t("booking:pickupAddressPlaceholder")}
                  value={values.pickupAddress}
                  onChange={(e) => setValues({ ...values, pickupAddress: e.target.value })}
                />
                {errors.pickupAddress && <p className="form-error">{errors.pickupAddress}</p>}
              </div>

              <div className="form-group">
                <label htmlFor="passengerCount">{t("booking:passengers")}</label>
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
                <label htmlFor="notes">{t("booking:notes")}</label>
                <br />
                <textarea
                  id="notes"
                  placeholder={t("booking:notesPlaceholder")}
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
                  <label htmlFor="customerFirstName">{t("booking:firstName")}</label>
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
                  <label htmlFor="customerLastName">{t("booking:lastName")}</label>
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
                <label htmlFor="customerEmail">{t("booking:email")}</label>
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
                <label htmlFor="customerPhone">{t("booking:phone")}</label>
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
              <dt>{t("booking:success.trip")}</dt>
              <dd>
                {selectedRoute.departureLocation} &rarr; {selectedRoute.destination}
              </dd>

              <dt>{t("booking:success.dateTime")}</dt>
              <dd>
                {values.bookingDate} at {values.pickupTime}
              </dd>

              <dt>{t("booking:pickupAddress")}</dt>
              <dd>{values.pickupAddress}</dd>

              <dt>{t("booking:passengers")}</dt>
              <dd>{values.passengerCount}</dd>

              {values.notes && (
                <>
                  <dt>{t("booking:notes")}</dt>
                  <dd>{values.notes}</dd>
                </>
              )}

              <dt>{t("booking:firstName")}</dt>
              <dd>
                {values.customerFirstName} {values.customerLastName}
              </dd>

              <dt>{t("booking:email")}</dt>
              <dd>{values.customerEmail}</dd>

              <dt>{t("booking:phone")}</dt>
              <dd>{values.customerPhone}</dd>

              <dt>{t("booking:estimatedPrice")}</dt>
              <dd>
                {selectedRoute.price.toFixed(2)} {selectedRoute.currency}
              </dd>
            </dl>
          )}

          {submitError && <p role="alert">{submitError}</p>}

          <div className="row" style={{ marginTop: "var(--space-6)" }}>
            {step > 1 && (
              <button type="button" className="btn-secondary" onClick={goToPreviousStep} disabled={isSubmitting}>
                {t("common:buttons.back")}
              </button>
            )}
            {step < 4 ? (
              <button type="submit">{t("common:buttons.next")}</button>
            ) : (
              <button type="button" onClick={handleConfirm} disabled={isSubmitting}>
                {isSubmitting ? t("booking:submitting") : t("booking:confirmBooking")}
              </button>
            )}
          </div>
        </form>
      )}
    </div>
  );
}

export default BookingPage;
