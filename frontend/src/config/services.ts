/**
 * The site's marketing service categories — migrated from the real ROI
 * Limousinen business (Prompt 18, section 3), verified live against the
 * company's existing website (roi-limousinen.ch): Airport Transfer, Corporate
 * Events, City Tours, Point-to-Point, Professional Chauffeurs. Not backed by a
 * database table yet (deliberately: section 20 says not to build a full CMS
 * for this prompt), so this is the one place they're listed rather than being
 * duplicated across HomePage/ServicesPage/Footer. Titles/descriptions come
 * from site.json (translated); `icon` is a plain glyph placeholder (section
 * 25 — no real photography was available to migrate) rendered with an
 * accessible label, never the only source of the service's identity.
 */
export interface ServiceDefinition {
  key: "airportTransfer" | "corporateEvents" | "cityTours" | "pointToPoint" | "professionalChauffeursService";
  icon: string;
}

export const SERVICES: ServiceDefinition[] = [
  { key: "airportTransfer", icon: "✈" },
  { key: "corporateEvents", icon: "🤝" },
  { key: "cityTours", icon: "🏙" },
  { key: "pointToPoint", icon: "📍" },
  { key: "professionalChauffeursService", icon: "🎩" },
];
