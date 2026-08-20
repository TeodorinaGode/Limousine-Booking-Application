import { useCallback, useEffect, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { createPayment, getPaymentStatus, retryPayment } from "../../services/paymentService";
import { ApiError } from "../../services/apiClient";
import { APP_BRAND_NAME } from "../../config/brand";
import type { PublicPaymentStatusDto } from "../../types/payment";

const RETRYABLE_STATUSES = new Set(["Failed", "Cancelled"]);

function PaymentStatusPage() {
  const { bookingReference } = useParams<{ bookingReference: string }>();
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token") ?? "";

  const [status, setStatus] = useState<PublicPaymentStatusDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isBusy, setIsBusy] = useState(false);

  const load = useCallback(async () => {
    if (!bookingReference || !token) {
      setError("This payment link is missing required information.");
      setIsLoading(false);
      return;
    }
    setIsLoading(true);
    setError(null);
    try {
      setStatus(await getPaymentStatus(bookingReference, token));
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setStatus(null);
      } else {
        setError(err instanceof Error ? err.message : "Failed to load payment status.");
      }
    } finally {
      setIsLoading(false);
    }
  }, [bookingReference, token]);

  useEffect(() => {
    load();
  }, [load]);

  const startCheckout = async (isRetry: boolean) => {
    if (!bookingReference || !token || isBusy) return;
    setError(null);
    setIsBusy(true);
    try {
      const checkout = isRetry ? await retryPayment(bookingReference, token) : await createPayment(bookingReference, token);
      window.location.href = checkout.checkoutUrl;
    } catch (err) {
      setError(err instanceof ApiError ? err.message : err instanceof Error ? err.message : "Failed to start payment.");
      setIsBusy(false);
    }
  };

  return (
    <div className="container container--narrow fade-in" style={{ paddingTop: "var(--space-16)", paddingBottom: "var(--space-16)" }}>
      <p className="site-nav__brand" style={{ marginBottom: "var(--space-6)" }}>
        <Link to="/">{APP_BRAND_NAME}</Link>
      </p>
      <div className="card">
        <h1>Payment Status</h1>
        {bookingReference && <p className="text-muted">Booking {bookingReference}</p>}

        {isLoading && <div className="skeleton skeleton-line" style={{ height: 60 }} />}
        {error && <p role="alert">{error}</p>}

        {!isLoading && !error && !status && (
          <>
            <p>No payment has been started for this booking yet.</p>
            <button type="button" onClick={() => startCheckout(false)} disabled={isBusy}>
              {isBusy ? "Redirecting to payment..." : "Pay Now"}
            </button>
          </>
        )}

        {!isLoading && status && (
          <>
            <dl>
              <dt>Status</dt>
              <dd role="status">{status.status}</dd>
              <dt>Amount</dt>
              <dd>
                {status.amount.toFixed(2)} {status.currency}
              </dd>
              {status.paidAt && (
                <>
                  <dt>Paid at</dt>
                  <dd>{new Date(status.paidAt).toLocaleString()}</dd>
                </>
              )}
            </dl>
            {RETRYABLE_STATUSES.has(status.status) && (
              <button type="button" onClick={() => startCheckout(true)} disabled={isBusy}>
                {isBusy ? "Redirecting to payment..." : "Retry Payment"}
              </button>
            )}
          </>
        )}
      </div>
      <p style={{ marginTop: "var(--space-6)" }}>
        <Link to="/">Return to home</Link>
      </p>
    </div>
  );
}

export default PaymentStatusPage;
