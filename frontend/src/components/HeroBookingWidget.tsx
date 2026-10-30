import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import TravelDateTimeField from "./TravelDateTimeField";
import type { PublicRouteDto } from "../types/booking";

interface HeroBookingWidgetProps {
  routes: PublicRouteDto[];
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function todayDateOnly(): Date {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
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
  const [bookingDate, setBookingDate] = useState(todayIso());
  const [pickupTime, setPickupTime] = useState("");
  const [passengerCount, setPassengerCount] = useState(1);

  // Only offered when the reverse trip is itself a real bookable route —
  // swapping into a dead-end From/To pair would be worse than no button.
  const canSwap = useMemo(
    () => Boolean(from && to && routes.some((r) => r.departureLocation === to && r.destination === from)),
    [routes, from, to]
  );

  const handleSwap = () => {
    if (!canSwap) return;
    setFrom(to);
    setTo(from);
  };

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
      <div className="hero-widget__route-pair">
        <div className="form-group">
          <label htmlFor="widget-from">{t("widget.from")}</label>
          <select id="widget-from" value={from} onChange={(e) => { setFrom(e.target.value); setTo(""); }}>
            <option value="">{t("widget.selectRoute")}</option>
            {fromOptions.map((location) => (
              <option key={location} value={location}>{location}</option>
            ))}
          </select>
        </div>

        <button
          type="button"
          className="hero-widget__swap"
          onClick={handleSwap}
          disabled={!canSwap}
          aria-label={t("widget.swap")}
          title={t("widget.swap")}
        >
          &#8646;
        </button>

        <div className="form-group">
          <label htmlFor="widget-to">{t("widget.to")}</label>
          <select id="widget-to" value={to} onChange={(e) => setTo(e.target.value)} disabled={!from}>
            <option value="">{t("widget.selectRoute")}</option>
            {toOptions.map((location) => (
              <option key={location} value={location}>{location}</option>
            ))}
          </select>
        </div>
      </div>

      <TravelDateTimeField
        date={bookingDate}
        time={pickupTime}
        minDate={todayDateOnly()}
        onApply={(date, time) => {
          setBookingDate(date);
          setPickupTime(time);
        }}
      />

      <div className="form-group">
        <label htmlFor="widget-passengers">{t("widget.passengers")}</label>
        <input
          id="widget-passengers"
          type="number"
          min={1}
          value={passengerCount}
          onChange={(e) => setPassengerCount(Number(e.target.value))}
        />
      </div>

      <button type="submit" className="hero-widget__submit">{t("widget.checkAvailability")}</button>
    </form>
  );
}

export default HeroBookingWidget;
