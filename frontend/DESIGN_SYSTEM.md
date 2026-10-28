# Limousinen — Design System

**Direction:** Mysterious luxury. Dark cinematic. Premium chauffeur. European. Minimal.

This document describes the system that already exists in `src/index.css` (built across Prompt 14's initial token layer, the Prompt 21 cinematic redesign, and follow-up passes), formalizes the gaps that were undocumented, and sets the rules for anything built on top of it going forward. It is a specification, not a rewrite — almost everything below is already implemented; each section says so explicitly and points at the real token/class names.

This codebase's component model is **CSS classes + tokens, not a wrapping React component library.** `.cta-noir--solid`, `.content-card`, `.noir-heading` etc. are the reusable units — applied directly in page markup rather than through a `<Button>`/`<Card>` component API. That's a deliberate, already-validated choice (confirmed with the client: extracting parallel React primitives for cosmetic-only benefit was rejected as unnecessary risk across ~10 already-tested pages). Everything in this system follows that same idiom.

---

## 1. Color Tokens

`:root` in `index.css:9–111`. Grayscale + a single restrained metallic accent — no random hues anywhere in the codebase (verified: zero hardcoded hex colors in any `.tsx` file).

| Token | Value | Use |
|---|---|---|
| `--color-background` | `#080808` | Page background |
| `--color-surface` | `#111111` | Cards, panels |
| `--color-surface-elevated` | `#181818` | Raised surfaces (modals, hovered elements) |
| `--color-surface-hover` | `#202020` | Interactive hover fill |
| `--color-border` / `--color-border-light` | `#2a2a2a` / `#383838` | Default and emphasized borders |
| `--color-text-primary` / `-secondary` / `-muted` | `#f5f5f5` / `#a3a3a3` / `#707070` | Three-tier text hierarchy |
| `--color-accent` / `-strong` | `#b8b8b8` / `#d8d8d8` | Brushed-silver accent, used sparingly (hover states, small highlights) — never a primary fill |
| `--noir-0..5`, `--noir-silver` | `#030303 → #181818`, `#c7c7c7` | Additive depth levels for the cinematic public sections (`.noir-section--black/--deep/--surface`) — layered *on top of* the base tokens, never replacing them, specifically so Admin/Driver (which only read the base tokens) can't be affected |
| `--color-status-*-bg/-border/-text` | grayscale + opacity | Status color is conveyed by **brightness/opacity, not hue** — confirmed/completed reads brighter and whiter, cancelled/pending reads dimmer. No green "success" or red "error" anywhere; this is the rule, not an oversight. |

The app is dark-only by design — there is no light theme or theme toggle. No component ever branches on theme; it only ever reads tokens.

**Rule:** never introduce a new hex color inline. If a new surface/text/border need arises, it's a new token in this block, documented here.

---

## 2. Typography System

Two families, each with one job — this is the one genuine gap this pass closed (previously a single typeface did both jobs):

- `--font-sans` = **Inter** (`index.html` Google Fonts link) — body copy, UI chrome, uppercase labels, buttons. Weights 400–800 loaded.
- `--font-display` = **Fraunces**, a serif (`index.html`) — headings, hero, section titles only. Weights 300/400/600 loaded (not 700/800 — synthetic bolding was avoided by only using loaded weights).

Applied to: `h1`, `.section__title`, `.cinematic-hero__title`, `.noir-heading` (`index.css:277, 1403, 2012–2028, 2096–2110`). Body text, `p`, form fields, and all uppercase labels (`.noir-label`, `h2`/`h3`) stay on Inter — the serif is reserved for the few large, emotional display moments, which is what makes it read as editorial rather than decorative.

**Fluid scale** — every display size uses `clamp()`, none are fixed px:

| Context | Value |
|---|---|
| Cinematic hero title | `clamp(2.75rem, 9vw, 6.5rem)` |
| `.noir-heading` | `clamp(2rem, 6vw, 4rem)` |
| `.section__title` | `clamp(1.5rem, 3vw, 2.25rem)` |
| Base `h1` | `clamp(2rem, 4vw, 2.75rem)` |

**Weight-as-contrast convention:** hero/noir headings pair a `font-weight: 300` italic "light" span with a `font-weight: 600` "strong" span on the next line (`.cinematic-hero__title-light/-strong`, `.noir-heading .light/.strong`) — e.g. *"Move" / "In Silence."* This light↔strong pairing is the signature display pattern; reuse it for any new large headline rather than inventing a new treatment.

**Labels:** `.noir-label` — 0.7rem, 700 weight, `letter-spacing: 0.4em`, uppercase, muted color. Same idiom as the base `h2`/`h3` (uppercase, tracked, secondary color). Never introduce a label style outside this pattern.

**Rule:** display font is for headlines only. If body text ever "needs" more presence, increase weight/size within Inter — don't reach for Fraunces at small sizes, it reads as clutter, not luxury.

---

## 3. Spacing System

`--space-1` through `--space-16` (`index.css:58–65`): **4, 8, 12, 16, 24, 32, 48, 64px.** A single geometric-ish scale used everywhere via `gap`/`padding`/`margin` — no raw pixel spacing in component CSS (verified by inspection; the few px values that exist are for borders/radii/icon sizing, not spacing).

**Rule:** always reach for a `--space-*` token. If a gap doesn't fit the scale, the layout is wrong before the spacing is — resize to the nearest token rather than inventing e.g. `18px`.

---

## 4. Grid System

`.grid`, `.grid--2/--3/--4`, `.grid--bento` (`index.css:1430–1478`) — all collapse to fewer columns at 900px, then to a single column at 560px. `.grid--bento` gives its first child a 2-column/2-row span for an editorial "featured item" layout.

Beyond the shared utility classes, a handful of components define their own `grid-template-columns` locally (trust items at 5 columns, footer at `2fr 1fr 1fr 1fr`, the map's split layout at `3fr 2fr`) — that's correct; those are structural to one component, not a repeatable pattern, so they don't belong in the shared utility.

**Rule — this is the one that most directly encodes "no excessive card grids":** grids (`.grid--*`) are for genuinely repeatable, equal-weight items (trust badges, FAQ entries, admin list rows). **Primary marketing content — Services, Fleet, Routes — uses editorial layout (§7), not a grid of cards.** If a new public section is tempted to reach for `.grid--3` of cards, that's the signal to design an editorial list/showcase instead.

Containers: `--container-wide` (1200px) / `-medium` (760px) / `-narrow` (440px) center all content — this, plus fluid typography, is *why* there's no breakpoint ladder above 900px (see §14).

---

## 5. Button System

Two deliberately separate families — document the distinction so a third never gets invented:

**A. Functional UI buttons** (`index.css:376–436`) — forms, admin/driver actions, anywhere a customer/staff member takes a concrete action:
- Base `button` — filled, `--color-text-primary` background / `--color-background` text (i.e. inverted — bright button on dark page). This is the default; **most buttons need no class at all.**
- `.btn-secondary` — outlined, transparent fill.
- `.btn-ghost` — no border until hover, for low-emphasis actions (Modal's close button).
- `.btn-danger` — outlined, muted (no red — grayscale danger, consistent with §1).
- `.btn-block` — full width.
- States: `:hover:not(:disabled)`, `:disabled` (opacity 0.4), `:focus-visible` (2px accent outline, 2px offset) — defined once on the base element, inherited by every variant.

**B. Cinematic marketing CTAs** (`.cta-noir--solid/--outline`, `index.css:2057–2085`) — public-site navigational/marketing calls to action (hero, section closers, "Book This Journey"). Larger padding, uppercase, tracked letter-spacing, arrow-glyph suffix (`<span aria-hidden="true"> →</span>` appended *after* the translated text — keeps the accessible name clean for tests/screen readers while showing the arrow visually), `translateY(-2px)` lift on hover.

**Rule:** `.btn-*` for transactional UI, `.cta-noir` for marketing intent. Never mix — a form's submit button is never `.cta-noir`, and a hero CTA is never a bare `button`.

---

## 6. Input / Form System

Base `input`/`select`/`textarea` styling (`index.css:438+`) shares the button family's token language (surface background, `--color-border`, `--radius-sm`, `:focus-visible` outline). Established conventions, already followed throughout `BookingPage.tsx` and every admin form:

- **Every field has a real `<label htmlFor>`** — never placeholder-only labeling (verified in the earlier audit).
- **Inline errors** use `role="alert"` with `.form-error` styling (`--color-text-primary`, not a muted/invisible color — this was a real bug fixed earlier this session).
- **Logical tab order** follows DOM order — no `tabIndex` overrides.
- Large touch targets via the shared `--space-*` padding scale, not custom per-field sizing.

**Rule:** one column on mobile (already true — form layouts use `flex-direction: column` below 900px, no side-by-side fields that would cramp at 375px).

---

## 7. Card Strategy

`.content-card` + `.content-card__media/__title/__desc/__meta` (`index.css:1540–1586`): `--radius-md` (6px — restrained, never pill-shaped), 1px border, hover lifts `-4px` with a soft shadow. This is the **only** card component in the system — no per-page card variants.

**Where cards belong:** FAQ entries, trust/credential badges, admin list rows — repeatable, equal-weight, secondary content.

**Where cards do NOT belong — this is the explicit "no excessive card grids" rule:** Services, Fleet, and (partially — see Known Gaps) Routes use **editorial layout** instead: large index numbers, big typography rows, hover-synced detail panels (`.editorial-list__row`, `.fleet-cinematic`, `.route-editorial`). A luxury brand's primary offerings shouldn't look like a SaaS pricing grid; editorial layout is what makes Services/Fleet feel like a magazine spread instead of a product catalog.

---

## 8. Modal Strategy

**One shared component, `components/Modal.tsx`**, used by every dialog in the app (7 admin/driver modals — booking assignment/edit, availability, password reset, driver/vehicle/route forms). No per-page modal markup exists or should exist.

- Dark surface: `--color-surface` background, `--color-border-light` border, `--radius-lg`, `--shadow-lg`.
- Overlay: `rgba(0,0,0,0.7)` + 2px blur, click-outside closes (click target === overlay, not a child).
- `role="dialog"` `aria-modal="true"` `aria-label={title}`.
- **Escape-to-close** (added in this pass — the one real gap in the existing modal strategy; previously only click-outside closed it).

The public site currently has no modals — every public interaction (booking, payment) is a full page/step, by design (§11 of the earlier design brief: booking should never feel like a popup interruption). If a public-site modal is ever needed (e.g. a fleet vehicle lightbox), it reuses this exact component — never a new one.

---

## 9. Navigation System

Two intentionally different navigation chromes for two intentionally different contexts:

- **Public (`Header.tsx`)** — the brand-facing nav. Transparent at page top, becomes dark glass (`site-header--scrolled`, backdrop blur) on scroll. Desktop: horizontal links + language dropdown + `.cta-noir` "Book a Ride". Mobile: full-screen dark dialog panel (`role="dialog"`), not a slide-out drawer — matches the cinematic full-bleed language used everywhere else on the public site.
- **Admin/Driver (`AdminNav.tsx`/`DriverNav.tsx`)** — a functional sidebar (`.app-nav`, 220px fixed, collapses to a horizontal scrollable bar ≤900px). Deliberately plainer — internal tool chrome, not brand storytelling. Shares the base tokens (so it's never visually broken) but never receives `Header`'s cinematic treatment (custom cursor, scroll-glass) — those are structurally gated to only mount inside `Header`.

**Rule:** never let Admin/Driver inherit public-site cinematic flourishes, and never simplify the public nav toward the admin sidebar's utilitarian style. They're different products wearing the same base tokens.

---

## 10. Section System

Public marketing pages are built from alternating depth bands, not uniform white-space — `.noir-section--black` (`--noir-0`) / `--deep` / `--surface` (`index.css:2116+`), each `--space-16` vertical padding. This alternation is what gives long scrolling pages a sense of depth/rhythm without needing images.

Heading hierarchy within a section: `.noir-label` (small, uppercase, tracked eyebrow) → `.noir-heading` or `.section__title` (the display-font headline) → body copy. Every section follows this same three-tier order; never skip the eyebrow label on a primary section, and never use two heading treatments in the same section.

---

## 11. Icon Strategy

**No icon library** — no Font Awesome, Material Icons, Lucide, or emoji anywhere in the codebase (verified: zero imports of any icon package). Instead, a small fixed set of typographic glyphs does every job:

| Glyph | Use |
|---|---|
| `→` (as `<span aria-hidden="true"> →</span>`) | CTA arrows |
| `☰` / `✕` | Mobile menu open/close |
| `☀` / `☾` | Theme toggle |
| `▾` / `▴` | Language dropdown caret |
| `×` | Modal close |

This is a deliberate, already-correct choice, not a gap — it matches "avoid icons where typography is sufficient" directly. **Rule:** before adding any icon anywhere, ask whether a word or a glyph from this exact set already says it. Do not install an icon package.

---

## 12. Image Strategy

**The hero now has a real photograph** — `public/images/hero-black-mercedes-night.jpg` (desktop) / `-mobile.jpg` (≤640px), a black Mercedes-Benz S-Class at night by Ilya Godze, Unsplash License. Applied as a `background-image` layer under the existing translucent gradient tints in `.cinematic-hero` (not an `<img>` tag — it's a full-bleed decorative background, with the same information conveyed in real text, so no `alt` is needed). **Every other hero/fleet/portrait panel still renders a CSS gradient standing in for a photo** (`.noir-portrait`, `.fleet-cinematic`, `.editorial-panel__image`) — no licensed image sourced for those yet.

**The contract for when more photography arrives** (so it drops in without restructuring, same pattern the hero photo just followed):
- Each placeholder panel already reserves its box via `aspect-ratio`/fixed dimensions and a `background:` property — swap the gradient for `background-image` (or an `<img>` with `object-fit: cover`) in that one declaration, nothing else changes.
- Every `<img>` gets a real, descriptive `alt` — never `alt=""` on content images (decorative-only images may use empty alt).
- `loading="lazy"` on any image below the fold (not needed above the fold — e.g. the hero — where lazy-loading would delay the largest contentful paint).
- No stock-photo clichés — mood should match the brief (black Mercedes at night, wet asphalt, Swiss architecture, chauffeur silhouettes), not generic "smiling business person" stock.
- Verify the license before adding anything — the hero photo used the Unsplash License (free commercial use); confirm equivalent terms for any future image.

---

## 13. Animation System

Three named transition tokens (`index.css:78–80`): `--transition-fast` (150ms), `--transition-base` (220ms), `--transition-cinematic` (900ms, `cubic-bezier(0.16, 1, 0.3, 1)` — a slow-out ease used only for the hero's staged reveal).

- **Scroll reveal** — `useScrollReveal` hook (`hooks/useScrollReveal.ts`), `IntersectionObserver`-based, one-time fade-up on entering viewport, jsdom-safe fallback.
- **Hero entrance** — `@keyframes cinematic-fade-up`, staggered via `animation-delay` per element (mark → title → actions), `index.css:2036+`.
- **Loading** — `.skeleton` shimmer (§16).
- **prefers-reduced-motion is respected globally** (`index.css:1153–1161` and per-animation, e.g. `2046–2053`) — every keyframe animation has a reduced-motion override that snaps to the end state instantly. This is non-negotiable and must carry to any new animation.

**Rule — "not excessive":** animate `opacity`/`transform` only (never layout-triggering properties), keep durations in the 150ms–1s range already established, and every new animated element needs a `prefers-reduced-motion` fallback before it ships, not after.

---

## 14. Responsive Breakpoints

Canonical scale (now documented as a comment block at the top of `index.css`, since CSS custom properties can't be referenced inside `@media` conditions): **480, 560, 640, 768, 900px.** Every media query in the file targets one of these five — confirmed by inspection, no stray one-off breakpoint values exist.

Beyond 900px, the layout doesn't add more breakpoints — it grows inside the centered `--container-wide` (1200px) and relies on `clamp()` typography to keep scaling smoothly up to 1920px+. This is deliberate: five breakpoints plus fluid type covers the full 320–1920px range the brief asks for, without a breakpoint-per-device-class approach that tends to produce inconsistent in-between states.

**Rule:** author mobile-first (base styles = smallest screen, `min-width`/`max-width` media queries layer up/down from there), and reuse one of the five canonical values — don't introduce a sixth without a documented reason.

---

## 15. Accessibility Rules

Already in place, listed here as the contract new work must maintain:

- **Focus-visible** outlines on every interactive element (`:focus-visible`, `index.css:397` and repeated per component) — never removed, never `outline: none` without a replacement.
- **Keyboard support:** modals close on Escape (§8, added this pass) and click-outside; dropdown menus (language selector) close on Escape and outside click; mobile nav is a real `role="dialog"`.
- **Semantic HTML + real labels** — `<label htmlFor>` on every form field, no placeholder-only labeling.
- **`role="alert"`** for errors, **`role="status"`** for success/status messaging — always paired with real text, never color-only.
- **`prefers-reduced-motion`** respected on every animation (§13).
- **Contrast:** the near-white-on-near-black grayscale system keeps text contrast high by construction; the one accent color is used for small highlights only, never for text-on-background contrast-critical content.

**Rule:** any new interactive element needs focus-visible + keyboard operability before it's considered done — not as a follow-up pass.

---

## 16. Loading States

`.skeleton` / `.skeleton-line` (`index.css:1081–1101`) — a shimmering gradient bar (`skeleton-shimmer` keyframe, 1.4s loop) standing in for content shape before it loads. Used on Home, Booking, Routes, Fleet, and Payment-status pages. **Rule:** any new async content gets a skeleton in its final shape, not a spinner and not a blank area — the skeleton *is* the loading-state design language here, don't introduce a spinner component alongside it.

---

## 17. Empty States

`.empty-state` / `.empty-state__title` (`index.css:1065–1077`) — centered, generously padded, muted body text with an uppercase tracked title (same visual family as `.noir-label`). Already used for "no routes available" / "no vehicles available" with real translated copy, never a silently blank section.

**Rule:** every list/collection view needs an explicit empty-state branch with real copy before it ships — "renders nothing" is not an acceptable empty state.

---

## 18. Error States

`role="alert"` + `.form-error` for field-level errors (fixed this session to use `--color-text-primary`, not a low-contrast gray — a real visibility bug). Page-level errors (failed fetch, payment failure) follow the same `role="alert"` pattern with a `[role="alert"]` bordered box (`index.css:503+`).

**Rule:** never a red error color — grayscale + a left-border accent + real explanatory copy, consistent with the no-random-colors rule in §1. Every error state must be screen-reader-announced (`role="alert"`), not just visually styled.

---

## 19. Success States

`role="status"` (`index.css:513+`) — success is communicated the same way "confirmed"/"completed" booking statuses are (§1): **brighter, whiter, more solid**, not a green checkmark. Payment confirmation copy is explicit ("Your payment has been confirmed") rather than relying on color alone. This is the deliberate house style: luxury brands signal success through restraint and clarity of language, not a traffic-light color system.

**Rule:** never introduce green for success — reuse the `--color-status-completed-*` brightness pattern.

---

## Global Rules (apply to every section above)

1. **One token layer, no per-component overrides.** If a component needs a color/spacing/radius that doesn't exist yet, add the token to `:root` — never hardcode.
2. **No rounded-pill anything.** Radius tops out at `--radius-lg` (8px). This is intentional restraint, not an oversight — recheck against this if a new component looks "too soft."
3. **Editorial over grid for primary content, grid for repeatable secondary content** (§4, §7) — the single biggest lever against looking like a generic template.
4. **Two button families, never a third** (§5). **One modal component, never a second** (§8). **One icon vocabulary, never a library** (§11).
5. **Every animation needs a `prefers-reduced-motion` fallback before merge.**
6. **Dark is the default identity; light is a fully-realized opt-in**, never a diluted version of dark — both must pass the same rules above independently.

## Known Gaps (not fixed by this pass — flagged, not silently left)

- **Fleet and chauffeur sections still have no real photography** — only the hero has one now (§12); nothing to implement for the rest without a licensed asset.
- **`RoutesPage.tsx`'s map-synced list still uses `.content-card`**, not editorial layout, while its own no-map fallback (same page) uses `.route-editorial` — a genuine inconsistency, not yet resolved (flagged in the prior audit; deferred pending your go-ahead since it touches a tested, map-synced interaction).
