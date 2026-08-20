import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { retryPayment } from "../../services/paymentService";
import { ApiError } from "../../services/apiClient";
import { APP_BRAND_NAME } from "../../config/brand";

function PaymentCancelledPage() {
  const { t } = useTranslation(["payment", "common"]);
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
      setError(err instanceof ApiError ? err.message : err instanceof Error ? err.message : t("common:misc.error"));
      setIsBusy(false);
    }
  };

  return (
    <div className="container container--narrow fade-in" style={{ paddingTop: "var(--space-16)", paddingBottom: "var(--space-16)", textAlign: "center" }}>
      <p className="site-nav__brand" style={{ marginBottom: "var(--space-6)" }}>
        <Link to="/">{APP_BRAND_NAME}</Link>
      </p>
      <div className="card">
        <h1>{t("payment:cancelledTitle")}</h1>
        <p role="status">{t("payment:cancelledMessage")}</p>
        {error && <p role="alert">{error}</p>}
        {bookingReference && token && (
          <button type="button" onClick={handleRetry} disabled={isBusy}>
            {isBusy ? t("payment:redirecting") : t("common:buttons.tryAgain")}
          </button>
        )}
      </div>
      <p style={{ marginTop: "var(--space-6)" }}>
        <Link to="/">{t("payment:returnHome")}</Link>
      </p>
    </div>
  );
}

export default PaymentCancelledPage;
