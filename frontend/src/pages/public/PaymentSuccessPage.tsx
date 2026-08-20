import { useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { getPaymentStatus } from "../../services/paymentService";
import { APP_BRAND_NAME } from "../../config/brand";
import type { PublicPaymentStatusDto } from "../../types/payment";

const POLL_INTERVAL_MS = 2000;
const MAX_POLL_ATTEMPTS = 15;

function PaymentSuccessPage() {
  const [searchParams] = useSearchParams();
  const bookingReference = searchParams.get("ref") ?? "";
  const token = searchParams.get("token") ?? "";

  const [status, setStatus] = useState<PublicPaymentStatusDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [gaveUp, setGaveUp] = useState(false);
  const attemptsRef = useRef(0);

  useEffect(() => {
    if (!bookingReference || !token) {
      setError("This payment link is missing required information.");
      return;
    }

    let cancelled = false;
    let timer: ReturnType<typeof setTimeout>;

    const poll = async () => {
      try {
        const result = await getPaymentStatus(bookingReference, token);
        if (cancelled) return;
        setStatus(result);

        // Only the webhook ever marks a payment Paid (never the browser returning
        // from checkout) — so this keeps polling until that has actually happened,
        // capped so a stalled/never-arriving webhook doesn't poll forever.
        if (result.status !== "Paid") {
          attemptsRef.current += 1;
          if (attemptsRef.current >= MAX_POLL_ATTEMPTS) {
            setGaveUp(true);
            return;
          }
          timer = setTimeout(poll, POLL_INTERVAL_MS);
        }
      } catch (err) {
        if (!cancelled) setError(err instanceof Error ? err.message : "Failed to load payment status.");
      }
    };

    poll();
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [bookingReference, token]);

  const isPaid = status?.status === "Paid";

  return (
    <div className="container container--narrow fade-in" style={{ paddingTop: "var(--space-16)", paddingBottom: "var(--space-16)", textAlign: "center" }}>
      <p className="site-nav__brand" style={{ marginBottom: "var(--space-6)" }}>
        <Link to="/">{APP_BRAND_NAME}</Link>
      </p>
      <div className="card">
        {error ? (
          <p role="alert">{error}</p>
        ) : isPaid ? (
          <>
            <div aria-hidden="true" style={{ fontSize: "1.5rem", marginBottom: "var(--space-3)" }}>
              ✓
            </div>
            <h1>Payment Successful</h1>
            <p role="status">
              Your payment of {status!.amount.toFixed(2)} {status!.currency} has been confirmed.
            </p>
          </>
        ) : gaveUp ? (
          <>
            <h1>Confirming Payment</h1>
            <p role="status">
              We're still waiting for confirmation from the payment provider. This can take a moment — please check back shortly, or view the
              current status below.
            </p>
            {bookingReference && (
              <p>
                <Link to={`/booking/payment/${bookingReference}?token=${token}`}>Check payment status</Link>
              </p>
            )}
          </>
        ) : (
          <>
            <h1>Confirming Payment</h1>
            <p role="status">Please wait while we confirm your payment...</p>
          </>
        )}
      </div>
      <p style={{ marginTop: "var(--space-6)" }}>
        <Link to="/">Return to home</Link>
      </p>
    </div>
  );
}

export default PaymentSuccessPage;
