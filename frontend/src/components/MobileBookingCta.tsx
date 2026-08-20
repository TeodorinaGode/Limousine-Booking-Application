import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";

/** Sticky bottom "Book a Ride" CTA shown only on narrow screens (Prompt 17, section 34) — included on marketing pages, not on the booking/payment flow itself where it would be redundant. */
function MobileBookingCta() {
  const { t } = useTranslation("site");
  return (
    <Link to="/booking" className="mobile-booking-cta">
      {t("nav.bookARide")}
    </Link>
  );
}

export default MobileBookingCta;
