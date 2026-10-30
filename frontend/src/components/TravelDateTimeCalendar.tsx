import { DayPicker } from "react-day-picker";
import type { Matcher } from "react-day-picker";
import { de, enUS, fr } from "react-day-picker/locale";
import "react-day-picker/style.css";

const DAY_PICKER_LOCALES: Record<string, typeof enUS> = { en: enUS, de, fr };

interface TravelDateTimeCalendarProps {
  language: string;
  selected: Date | undefined;
  defaultMonth: Date | undefined;
  onSelect: (date: Date | undefined) => void;
  disabled: Matcher | Matcher[] | undefined;
}

/** Lazy-loaded (see TravelDateTimeField) so react-day-picker + its locale data never load until the picker is actually opened. */
function TravelDateTimeCalendar({ language, selected, defaultMonth, onSelect, disabled }: TravelDateTimeCalendarProps) {
  return (
    <DayPicker
      mode="single"
      selected={selected}
      defaultMonth={defaultMonth}
      onSelect={onSelect}
      disabled={disabled}
      locale={DAY_PICKER_LOCALES[language] ?? enUS}
      showOutsideDays
    />
  );
}

export default TravelDateTimeCalendar;
