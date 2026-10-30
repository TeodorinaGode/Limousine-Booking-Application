import { lazy, Suspense, useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import Modal from "./Modal";

const TravelDateTimeCalendar = lazy(() => import("./TravelDateTimeCalendar"));

const HOURS = Array.from({ length: 24 }, (_, i) => String(i).padStart(2, "0"));
const MINUTES = Array.from({ length: 60 }, (_, i) => String(i).padStart(2, "0"));

/** Scrolls the selected option in a time-select column into view (open or value change) — a no-op under jsdom, which doesn't implement scrollIntoView. */
function scrollOptionIntoView(container: HTMLDivElement | null, value: string) {
  const option = container?.querySelector<HTMLButtonElement>(`[data-value="${value}"]`);
  if (typeof option?.scrollIntoView === "function") {
    option.scrollIntoView({ block: "center" });
  }
}

interface TimeSelectProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
}

/**
 * Replaces the native <input type="time"> with a custom dark-themed
 * hour/minute picker — the native control's OS-rendered dropdown (light
 * background, system font) clashed badly with this modal's dark SBB-inspired
 * styling and could visually overlap the calendar above it.
 */
function TimeSelect({ label, value, onChange }: TimeSelectProps) {
  const [hour, minute] = value ? value.split(":") : ["", ""];
  const hourColRef = useRef<HTMLDivElement>(null);
  const minuteColRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    scrollOptionIntoView(hourColRef.current, hour || "00");
    scrollOptionIntoView(minuteColRef.current, minute || "00");
    // Only re-run when the picker first mounts (i.e. the modal opens) — scrolling on every keystroke-equivalent selection would fight the user's own scroll position.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="form-group" style={{ marginTop: "var(--space-2)" }}>
      <span className="form-group__label" id="widget-time-label">
        {label}
      </span>
      <div className="time-select" role="group" aria-labelledby="widget-time-label">
        <div className="time-select__col" role="group" aria-label="Hour" ref={hourColRef}>
          {HOURS.map((h) => (
            <button
              key={h}
              type="button"
              data-value={h}
              className={`time-select__option${h === hour ? " time-select__option--selected" : ""}`}
              aria-pressed={h === hour}
              onClick={() => onChange(`${h}:${minute || "00"}`)}
            >
              {h}
            </button>
          ))}
        </div>
        <span className="time-select__sep" aria-hidden="true">
          :
        </span>
        <div className="time-select__col" role="group" aria-label="Minute" ref={minuteColRef}>
          {MINUTES.map((m) => (
            <button
              key={m}
              type="button"
              data-value={m}
              className={`time-select__option${m === minute ? " time-select__option--selected" : ""}`}
              aria-pressed={m === minute}
              onClick={() => onChange(`${hour || "00"}:${m}`)}
            >
              {m}
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

function toDateOnly(value: string): Date | undefined {
  if (!value) return undefined;
  const [year, month, day] = value.split("-").map(Number);
  if (!year || !month || !day) return undefined;
  return new Date(year, month - 1, day);
}

/** Local-date formatting (never toISOString, which is UTC and can shift the day near midnight). */
function formatDateOnly(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatDisplayDate(value: string, locale: string): string {
  const date = toDateOnly(value);
  if (!date) return "";
  return date.toLocaleDateString(locale, { weekday: "short", day: "2-digit", month: "short", year: "numeric" });
}

interface TravelDateTimeFieldProps {
  /** "yyyy-MM-dd" or "" */
  date: string;
  /** "HH:mm" or "" */
  time: string;
  minDate?: Date;
  onApply: (date: string, time: string) => void;
  /** Overrides the generic "Date"/"Time" trigger labels — e.g. the booking page's more specific "Booking date"/"Pickup time". */
  dateLabel?: string;
  timeLabel?: string;
}

/**
 * Replaces native date/time inputs with a single combined picker modal
 * (inspired by sbb.ch's "Date of travel" dialog) — two trigger fields that
 * both open the same calendar + time modal, applied together on "Apply" so
 * a half-picked state never leaks into the parent form. The calendar itself
 * (react-day-picker + locale data) is lazy-loaded — see
 * TravelDateTimeCalendar — so it never costs anything on initial page load,
 * matching the existing ServiceAreaMap lazy-load precedent.
 */
function TravelDateTimeField({ date, time, minDate, onApply, dateLabel, timeLabel }: TravelDateTimeFieldProps) {
  const { t, i18n } = useTranslation("site");
  const [isOpen, setIsOpen] = useState(false);
  const [pendingDate, setPendingDate] = useState<Date | undefined>(undefined);
  const [pendingTime, setPendingTime] = useState("");

  const language = i18n.resolvedLanguage ?? "en";
  const resolvedDateLabel = dateLabel ?? t("widget.date");
  const resolvedTimeLabel = timeLabel ?? t("widget.time");

  const openModal = () => {
    setPendingDate(toDateOnly(date));
    setPendingTime(time);
    setIsOpen(true);
  };

  const handleApply = () => {
    onApply(pendingDate ? formatDateOnly(pendingDate) : "", pendingTime);
    setIsOpen(false);
  };

  return (
    <>
      <div className="form-group">
        <label htmlFor="widget-date-trigger">{resolvedDateLabel}</label>
        <button type="button" id="widget-date-trigger" className="date-time-trigger" onClick={openModal}>
          {date ? formatDisplayDate(date, language) : t("widget.selectDate")}
        </button>
      </div>

      <div className="form-group">
        <label htmlFor="widget-time-trigger">{resolvedTimeLabel}</label>
        <button type="button" id="widget-time-trigger" className="date-time-trigger" onClick={openModal}>
          {time || t("widget.selectTime")}
        </button>
      </div>

      {isOpen && (
        <Modal title={t("widget.dateOfTravel")} onClose={() => setIsOpen(false)}>
          <div className="app-datepicker">
            <Suspense fallback={<div className="skeleton skeleton-line" style={{ height: 280 }} />}>
              <TravelDateTimeCalendar
                language={language}
                selected={pendingDate}
                defaultMonth={pendingDate ?? minDate}
                onSelect={setPendingDate}
                disabled={minDate ? { before: minDate } : undefined}
              />
            </Suspense>
          </div>

          <TimeSelect label={resolvedTimeLabel} value={pendingTime} onChange={setPendingTime} />

          <div className="row" style={{ justifyContent: "flex-end", marginTop: "var(--space-3)" }}>
            <button type="button" className="btn-secondary" onClick={() => setIsOpen(false)}>
              {t("widget.cancel")}
            </button>
            <button type="button" onClick={handleApply} disabled={!pendingDate}>
              {t("widget.apply")}
            </button>
          </div>
        </Modal>
      )}
    </>
  );
}

export default TravelDateTimeField;
