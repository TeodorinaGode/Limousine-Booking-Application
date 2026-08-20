import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { retryPayment } from "../../services/paymentService";
import { ApiError } from "../../services/apiClient";
import { APP_BRAND_NAME } from "../../config/brand";

function PaymentCancelledPage() {
  const [searchParams] = useSearchParams();
  const bookingReference = searchParams.get("ref") ?? "";
  const token = searchParams.get("token") ?? "";

  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleRetry = async () => {
    if (!bookingReference || !token || isBusy) return;
    setError(null);
    setIsBusy(true);
    try {
      const checkout = await retryPayment(bookingReference, token);
      window.location.href = checkout.checkoutUrl;
    } catch (err) {
      setError(err instanceof ApiError ? err.message : err instanceof Error ? err.message : "Failed to start payment.");
      setIsBusy(false);
    }
  };

  return (
    <div className="container container--narrow fade-in" style={{ paddingTop: "var(--space-16)", paddingBottom: "var(--space-16)", textAlign: "center" }}>
      <p className="site-nav__brand" style={{ marginBottom: "var(--space-6)" }}>
        <Link to="/">{APP_BRAND_NAME}</Link>
      </p>
      <div className="card">
        <h1>Payment Cancelled</h1>
        <p role="status">Your payment was not completed. Your booking has not been charged.</p>
        {error && <p role="alert">{error}</p>}
        {bookingReference && token && (
          <button type="button" onClick={handleRetry} disabled={isBusy}>
            {isBusy ? "Redirecting to payment..." : "Try Again"}
          </button>
        )}
      </div>
      <p style={{ marginTop: "var(--space-6)" }}>
        <Link to="/">Return to home</Link>
      </p>
    </div>
  );
}

export default PaymentCancelledPage;
