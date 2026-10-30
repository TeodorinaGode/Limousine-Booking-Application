import { useEffect, useState, type FormEvent } from "react";
import { Link, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { createBooking, getActiveRoutes } from "../../services/bookingService";
import { createPayment } from "../../services/paymentService";
import { ApiError } from "../../services/apiClient";
import Header from "../../components/Header";
import TravelDateTimeField from "../../components/TravelDateTimeField";
import type { BookingDto, PublicRouteDto } from "../../types/booking";

/** Carried in via router state from the hero widget (section 27) or a Routes-page "Book This Route" CTA (section 26) — every field is optional since either flow may only supply some of them. */
interface BookingPrefillState {
  routeId?: string;
  bookingDate?: string;
  pickupTime?: string;
  passengerCount?: number;
}

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

type Step = 1 | 2 | 3;
const DRAFT_STORAGE_KEY = "limousine-booking.draft";
interface BookingDraft {
  step: Step;
  values: FormValues;
}

function readDraft(): BookingDraft | null {
  try {
    const raw = sessionStorage.getItem(DRAFT_STORAGE_KEY);
    return raw ? (JSON.parse(raw) as BookingDraft) : null;
  } catch {
    return null;
  }
}

function clearDraft() {
  sessionStorage.removeItem(DRAFT_STORAGE_KEY);
}

/** A draft with nothing but the untouched defaults isn't worth telling the
 * customer we "restored" — only resume (and announce) one with real content. */
function isMeaningfulDraft(draft: BookingDraft): boolean {
  return (
    draft.step > 1 ||
    draft.values.routeId !== "" ||
    draft.values.customerFirstName.trim() !== "" ||
    draft.values.customerEmail.trim() !== ""
  );
}

/** The 5 named phases shown in the progress indicator — Journey/Details/Review map
 * to the 3 form steps below; Payment/Confirmation happen after booking creation
 * (still on this page for Payment, then via the Stripe redirect for Confirmation),
 * so they're tracked separately from `step` (see `activePhaseIndex`). */
const PHASE_KEYS = ["journey", "details", "review", "payment", "confirmation"] as const;

function BookingProgress({ activeIndex, t }: { activeIndex: number; t: TFunction }) {
  return (
    <div>
      <div className="booking-progress" role="list" aria-label={t("booking:title")}>
        {PHASE_KEYS.map((key, index) => {
          const isActive = index === activeIndex;
          const isDone = index < activeIndex;
          return (
            <div
              key={key}
              className={`booking-progress__step${isActive ? " booking-progress__step--active" : ""}${isDone ? " booking-progress__step--done" : ""}`}
              role="listitem"
              aria-current={isActive ? "step" : undefined}
            >
              <span className="booking-progress__badge" aria-hidden="true">{isDone ? "✓" : index + 1}</span>
              <span className="booking-progress__label">{t(`booking:phases.${key}`)}</span>
              {index < PHASE_KEYS.length - 1 && <span className="booking-progress__line" />}
            </div>
          );
        })}
      </div>
      {/* Mobile-only (≤640px, see .booking-progress__current in index.css) —
       * the inline per-step label is hidden on narrow phones (it has no width
       * limit and could overflow), replaced by this single wrapping-safe line
       * naming the current phase, so "where am I" stays answered either way. */}
      <p className="booking-progress__current" aria-live="polite">{t(`booking:phases.${PHASE_KEYS[activeIndex]}`)}</p>
    </div>
  );
}

function validateJourney(values: FormValues, t: TFunction): Record<string, string> {
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

/** Validates every field on the merged "Details" step — pickup details and
 * customer contact information together on one screen, rather than two
 * separate steps, so the flow stays to 3 real form steps (Journey / Details /
 * Review) before Payment and Confirmation. */
function validateDetails(values: FormValues, t: TFunction): Record<string, string> {
  const errors: Record<string, string> = {};
  if (!values.pickupAddress.trim()) errors.pickupAddress = t("validation:pickupAddressRequired");
  if (!Number.isFinite(values.passengerCount) || values.passengerCount < 1) {
    errors.passengerCount = t("validation:passengersRequired");
  }
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
  const location = useLocation();
  const prefill = location.state as BookingPrefillState | null;

  const [routes, setRoutes] = useState<PublicRouteDto[]>([]);
  const [isLoadingRoutes, setIsLoadingRoutes] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  // An explicit prefill (homepage widget / "Book This Route") always means a
  // deliberate fresh start — it wins over any leftover draft from an earlier,
  // abandoned attempt rather than silently merging with it.
  const resumedDraft = prefill ? null : readDraft();
  const [showResumedNotice, setShowResumedNotice] = useState(
    () => resumedDraft !== null && isMeaningfulDraft(resumedDraft)
  );

  const [step, setStep] = useState<Step>(() => resumedDraft?.step ?? 1);
  const [values, setValues] = useState<FormValues>(() => {
    if (resumedDraft) return resumedDraft.values;
    return {
      ...initialValues,
      routeId: prefill?.routeId ?? initialValues.routeId,
      bookingDate: prefill?.bookingDate ?? initialValues.bookingDate,
      pickupTime: prefill?.pickupTime ?? initialValues.pickupTime,
      passengerCount: prefill?.passengerCount ?? initialValues.passengerCount,
    };
  });
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

  // Keeps the in-progress form recoverable across an accidental reload/crash —
  // cleared the moment a booking actually gets created (see handleConfirm).
  useEffect(() => {
    sessionStorage.setItem(DRAFT_STORAGE_KEY, JSON.stringify({ step, values }));
  }, [step, values]);

  const selectedRoute = routes.find((r) => r.id === values.routeId);

  /** Updates one field and immediately clears its own error, if any — so a
   * validation message disappears the moment the customer starts fixing it,
   * rather than staying stale until the next "Next" click re-validates. */
  const updateField = <K extends keyof FormValues>(key: K, value: FormValues[K]) => {
    setValues((current) => ({ ...current, [key]: value }));
    setErrors((current) => {
      if (!(key in current)) return current;
      const next = { ...current };
      delete next[key];
      return next;
    });
  };

  const goToNextStep = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const stepErrors = step === 1 ? validateJourney(values, t) : validateDetails(values, t);
    setErrors(stepErrors);
    if (Object.keys(stepErrors).length > 0) return;

    setStep((current) => (current < 3 ? ((current + 1) as Step) : current));
  };

  const goToPreviousStep = () => {
    setErrors({});
    setStep((current) => (current > 1 ? ((current - 1) as Step) : current));
  };

  const handleStartOver = () => {
    clearDraft();
    setShowResumedNotice(false);
    setErrors({});
    setStep(1);
    setValues(initialValues);
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
      clearDraft();
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
      <>
      <Header />
      <div className="container container--medium" style={{ paddingTop: "var(--space-8)" }}>
        <BookingProgress activeIndex={3} t={t} />
      </div>
      <div className="container container--narrow fade-in" style={{ paddingBottom: "var(--space-16)", textAlign: "center" }}>
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
      </>
    );
  }

  return (
    <>
    <Header />
    <div className="container container--medium" style={{ paddingTop: "var(--space-8)", paddingBottom: "var(--space-16)" }}>
      <h1>{t("booking:title")}</h1>

      <BookingProgress activeIndex={step - 1} t={t} />

      {showResumedNotice && (
        <p role="status" style={{ display: "flex", alignItems: "center", gap: "var(--space-3)", flexWrap: "wrap" }}>
          <span>We've restored your in-progress booking.</span>
          <button type="button" className="btn-ghost" onClick={handleStartOver}>
            Start Over
          </button>
        </p>
      )}

      {loadError && <p role="alert">{loadError}</p>}

      {isLoadingRoutes ? (
        <div className="stack">
          <div className="skeleton skeleton-line" style={{ height: 90 }} />
          <div className="skeleton skeleton-line" style={{ height: 90 }} />
        </div>
      ) : (
        <form onSubmit={step < 3 ? goToNextStep : (e) => e.preventDefault()} noValidate>
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
                        style={{ textAlign: "left", width: "100%" }}
                        onClick={() => updateField("routeId", route.id)}
                      >
                        {isSelected && (
                          <span className="trip-card__check" aria-hidden="true">
                            &#10003;
                          </span>
                        )}
                        <div className="trip-card__route">
                          <span>{route.departureLocation}</span>
                          <span className="trip-card__arrow">&rarr;</span>
                          <span>{route.destination}</span>
                        </div>
                      </button>
                    );
                  })}
                </div>
              </div>

              <div className="row">
                <TravelDateTimeField
                  date={values.bookingDate}
                  time={values.pickupTime}
                  minDate={new Date(new Date().getFullYear(), new Date().getMonth(), new Date().getDate())}
                  dateLabel={t("booking:date")}
                  timeLabel={t("booking:time")}
                  onApply={(date, time) => {
                    updateField("bookingDate", date);
                    updateField("pickupTime", time);
                  }}
                />
              </div>
              {(errors.bookingDate || errors.pickupTime) && (
                <div className="row">
                  {errors.bookingDate && <p className="form-error">{errors.bookingDate}</p>}
                  {errors.pickupTime && <p className="form-error">{errors.pickupTime}</p>}
                </div>
              )}
            </>
          )}

          {step === 2 && (
            <>
              <p className="section__eyebrow" style={{ marginTop: 0 }}>{t("booking:steps.pickupDetails")}</p>

              <div className="form-group">
                <label htmlFor="pickupAddress">{t("booking:pickupAddress")}</label>
                <br />
                <input
                  id="pickupAddress"
                  type="text"
                  autoComplete="street-address"
                  placeholder={t("booking:pickupAddressPlaceholder")}
                  value={values.pickupAddress}
                  onChange={(e) => updateField("pickupAddress", e.target.value)}
                />
                {errors.pickupAddress && <p className="form-error">{errors.pickupAddress}</p>}
              </div>

              <div className="form-group">
                <label htmlFor="passengerCount">{t("booking:passengers")}</label>
                <br />
                <input
                  id="passengerCount"
                  type="number"
                  inputMode="numeric"
                  min={1}
                  value={values.passengerCount}
                  onChange={(e) => updateField("passengerCount", Number(e.target.value))}
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
                  onChange={(e) => updateField("notes", e.target.value)}
                />
              </div>

              <p className="section__eyebrow" style={{ marginTop: "var(--space-6)" }}>{t("booking:steps.yourInformation")}</p>

              <div className="row">
                <div className="form-group">
                  <label htmlFor="customerFirstName">{t("booking:firstName")}</label>
                  <br />
                  <input
                    id="customerFirstName"
                    type="text"
                    autoComplete="given-name"
                    value={values.customerFirstName}
                    onChange={(e) => updateField("customerFirstName", e.target.value)}
                  />
                  {errors.customerFirstName && <p className="form-error">{errors.customerFirstName}</p>}
                </div>

                <div className="form-group">
                  <label htmlFor="customerLastName">{t("booking:lastName")}</label>
                  <br />
                  <input
                    id="customerLastName"
                    type="text"
                    autoComplete="family-name"
                    value={values.customerLastName}
                    onChange={(e) => updateField("customerLastName", e.target.value)}
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
                  inputMode="email"
                  autoComplete="email"
                  value={values.customerEmail}
                  onChange={(e) => updateField("customerEmail", e.target.value)}
                />
                {errors.customerEmail && <p className="form-error">{errors.customerEmail}</p>}
              </div>

              <div className="form-group">
                <label htmlFor="customerPhone">{t("booking:phone")}</label>
                <br />
                <input
                  id="customerPhone"
                  type="tel"
                  inputMode="tel"
                  autoComplete="tel"
                  value={values.customerPhone}
                  onChange={(e) => updateField("customerPhone", e.target.value)}
                />
                {errors.customerPhone && <p className="form-error">{errors.customerPhone}</p>}
              </div>
            </>
          )}

          {step === 3 && selectedRoute && (
            <>
              <h2 style={{ marginTop: 0 }}>{t("booking:review")}</h2>
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
              </dl>
            </>
          )}

          {submitError && <p role="alert">{submitError}</p>}

          <div className="row" style={{ marginTop: "var(--space-6)" }}>
            {step > 1 && (
              <button type="button" className="btn-secondary" onClick={goToPreviousStep} disabled={isSubmitting}>
                {t("common:buttons.back")}
              </button>
            )}
            {step < 3 ? (
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
    </>
  );
}

export default BookingPage;
