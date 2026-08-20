import { useCallback, useEffect, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { createPayment, getPaymentStatus, retryPayment } from "../../services/paymentService";
import { ApiError } from "../../services/apiClient";
import { APP_BRAND_NAME } from "../../config/brand";
import type { PublicPaymentStatusDto } from "../../types/payment";

const RETRYABLE_STATUSES = new Set(["Failed", "Cancelled"]);

function PaymentStatusPage() {
  const { t, i18n } = useTranslation(["payment", "common"]);
  const { bookingReference } = useParams<{ bookingReference: string }>();
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token") ?? "";

  const [status, setStatus] = useState<PublicPaymentStatusDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isBusy, setIsBusy] = useState(false);

  const load = useCallback(async () => {
    if (!bookingReference || !token) {
      setError(t("payment:missingLink"));
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
        setError(err instanceof Error ? err.message : t("common:misc.error"));
      }
    } finally {
      setIsLoading(false);
    }
  }, [bookingReference, token, t]);

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
      setError(err instanceof ApiError ? err.message : err instanceof Error ? err.message : t("common:misc.error"));
      setIsBusy(false);
    }
  };

  const statusLabel = status && i18n.exists(`common:status.payment.${status.status}`)
    ? t(`common:status.payment.${status.status}`)
    : status?.status;

  return (
    <div className="container container--narrow fade-in" style={{ paddingTop: "var(--space-16)", paddingBottom: "var(--space-16)" }}>
      <p className="site-nav__brand" style={{ marginBottom: "var(--space-6)" }}>
        <Link to="/">{APP_BRAND_NAME}</Link>
      </p>
      <div className="card">
        <h1>{t("payment:statusTitle")}</h1>
        {bookingReference && <p className="text-muted">{bookingReference}</p>}

        {isLoading && <div className="skeleton skeleton-line" style={{ height: 60 }} />}
        {error && <p role="alert">{error}</p>}

        {!isLoading && !error && !status && (
          <>
            <p>{t("payment:noPaymentYet")}</p>
            <button type="button" onClick={() => startCheckout(false)} disabled={isBusy}>
              {isBusy ? t("payment:redirecting") : t("payment:payNow")}
            </button>
          </>
        )}

        {!isLoading && status && (
          <>
            <dl>
              <dt>{t("payment:status")}</dt>
              <dd role="status">{statusLabel}</dd>
              <dt>{t("payment:amount")}</dt>
              <dd>
                {status.amount.toFixed(2)} {status.currency}
              </dd>
              {status.paidAt && (
                <>
                  <dt>{t("payment:paidAt")}</dt>
                  <dd>{new Date(status.paidAt).toLocaleString()}</dd>
                </>
              )}
            </dl>
            {RETRYABLE_STATUSES.has(status.status) && (
              <button type="button" onClick={() => startCheckout(true)} disabled={isBusy}>
                {isBusy ? t("payment:redirecting") : t("payment:retryPayment")}
              </button>
            )}
          </>
        )}
      </div>
      <p style={{ marginTop: "var(--space-6)" }}>
        <Link to="/">{t("payment:returnHome")}</Link>
      </p>
    </div>
  );
}

export default PaymentStatusPage;
