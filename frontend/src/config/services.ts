/**
 * The site's marketing service categories (Prompt 17, section 6) — not backed
 * by a database table yet (deliberately: section 41 says not to build a full
 * CMS for this prompt), so this is the one place they're listed rather than
 * being duplicated across HomePage/ServicesPage/Footer. Titles/descriptions
 * come from site.json (translated); `icon` is a plain glyph placeholder
 * (section 31 — no real photography is available yet) rendered with an
 * accessible label, never the only source of the service's identity.
 */
export interface ServiceDefinition {
  key: "airport" | "business" | "cityToCity" | "corporate" | "events" | "privateChauffeur";
  icon: string;
}

export const SERVICES: ServiceDefinition[] = [
  { key: "airport", icon: "✈" },
  { key: "business", icon: "💼" },
  { key: "cityToCity", icon: "🏙" },
  { key: "corporate", icon: "🤝" },
  { key: "events", icon: "✦" },
  { key: "privateChauffeur", icon: "🚗" },
];
