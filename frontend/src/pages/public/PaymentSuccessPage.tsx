import { useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { getPaymentStatus } from "../../services/paymentService";
import { APP_BRAND_NAME } from "../../config/brand";
import type { PublicPaymentStatusDto } from "../../types/payment";

const POLL_INTERVAL_MS = 2000;
const MAX_POLL_ATTEMPTS = 15;

function PaymentSuccessPage() {
  const { t } = useTranslation(["payment", "common"]);
  const [searchParams] = useSearchParams();
  const bookingReference = searchParams.get("ref") ?? "";
  const token = searchParams.get("token") ?? "";

  const [status, setStatus] = useState<PublicPaymentStatusDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [gaveUp, setGaveUp] = useState(false);
  const attemptsRef = useRef(0);

  useEffect(() => {
    if (!bookingReference || !token) {
      setError(t("payment:missingLink"));
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
        if (!cancelled) setError(err instanceof Error ? err.message : t("common:misc.error"));
      }
    };

    poll();
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
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
            <h1>{t("payment:successTitle")}</h1>
            <p role="status">
              {t("payment:successMessage", { amount: `${status!.amount.toFixed(2)} ${status!.currency}` })}
            </p>
          </>
        ) : gaveUp ? (
          <>
            <h1>{t("payment:confirming")}</h1>
            <p role="status">{t("payment:stillWaiting")}</p>
            {bookingReference && (
              <p>
                <Link to={`/booking/payment/${bookingReference}?token=${token}`}>{t("payment:checkStatus")}</Link>
              </p>
            )}
          </>
        ) : (
          <>
            <h1>{t("payment:confirming")}</h1>
            <p role="status">{t("payment:pleaseWait")}</p>
          </>
        )}
      </div>
      <p style={{ marginTop: "var(--space-6)" }}>
        <Link to="/">{t("payment:returnHome")}</Link>
      </p>
    </div>
  );
}

export default PaymentSuccessPage;
