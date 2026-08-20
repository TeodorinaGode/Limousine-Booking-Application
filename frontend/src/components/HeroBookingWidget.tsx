import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import type { PublicRouteDto } from "../types/booking";

interface HeroBookingWidgetProps {
  routes: PublicRouteDto[];
}

/**
 * Compact FROM/TO/DATE/TIME/PASSENGERS widget shown in/below the hero
 * (Prompt 17, section 4) — deliberately not a full booking form. On submit it
 * navigates into the existing booking flow with whatever was entered carried
 * along as router state (section 27), rather than re-implementing any
 * booking logic here. If the selected From/To pair matches a real active
 * route, that route is preselected; otherwise the customer just lands on
 * step 1 with their date/time/passengers already filled in.
 */
function HeroBookingWidget({ routes }: HeroBookingWidgetProps) {
  const { t } = useTranslation(["site", "booking"]);
  const navigate = useNavigate();

  const fromOptions = useMemo(
    () => Array.from(new Set(routes.map((r) => r.departureLocation))).sort(),
    [routes]
  );

  const [from, setFrom] = useState("");
  const toOptions = useMemo(
    () => Array.from(new Set(routes.filter((r) => r.departureLocation === from).map((r) => r.destination))).sort(),
    [routes, from]
  );

  const [to, setTo] = useState("");
  const [bookingDate, setBookingDate] = useState("");
  const [pickupTime, setPickupTime] = useState("");
  const [passengerCount, setPassengerCount] = useState(1);

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const matchedRoute = routes.find((r) => r.departureLocation === from && r.destination === to);

    navigate("/booking", {
      state: {
        routeId: matchedRoute?.id,
        bookingDate: bookingDate || undefined,
        pickupTime: pickupTime || undefined,
        passengerCount,
      },
    });
  };

  return (
    <form className="hero-widget fade-in" onSubmit={handleSubmit} aria-label={t("widget.checkAvailability")}>
      <div className="form-group">
        <label htmlFor="widget-from">{t("widget.from")}</label>
        <select id="widget-from" value={from} onChange={(e) => { setFrom(e.target.value); setTo(""); }}>
          <option value="">{t("widget.selectRoute")}</option>
          {fromOptions.map((location) => (
            <option key={location} value={location}>{location}</option>
          ))}
        </select>
      </div>

      <div className="form-group">
        <label htmlFor="widget-to">{t("widget.to")}</label>
        <select id="widget-to" value={to} onChange={(e) => setTo(e.target.value)} disabled={!from}>
          <option value="">{t("widget.selectRoute")}</option>
          {toOptions.map((location) => (
            <option key={location} value={location}>{location}</option>
          ))}
        </select>
      </div>

      <div className="form-group">
        <label htmlFor="widget-date">{t("widget.date")}</label>
        <input id="widget-date" type="date" value={bookingDate} onChange={(e) => setBookingDate(e.target.value)} />
      </div>

      <div className="form-group">
        <label htmlFor="widget-time">{t("widget.time")}</label>
        <input id="widget-time" type="time" value={pickupTime} onChange={(e) => setPickupTime(e.target.value)} />
      </div>

      <div className="form-group" style={{ display: "flex", gap: "var(--space-2)", alignItems: "flex-end" }}>
        <div style={{ flex: 1 }}>
          <label htmlFor="widget-passengers">{t("widget.passengers")}</label>
          <input
            id="widget-passengers"
            type="number"
            min={1}
            value={passengerCount}
            onChange={(e) => setPassengerCount(Number(e.target.value))}
          />
        </div>
        <button type="submit" style={{ flexShrink: 0 }}>{t("widget.checkAvailability")}</button>
      </div>
    </form>
  );
}

export default HeroBookingWidget;
