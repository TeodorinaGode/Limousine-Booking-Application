import type { Testimonial } from "../types/testimonial";

interface TestimonialCardProps {
  testimonial: Testimonial;
}

/**
 * Reusable testimonial card (Prompt 18, section 18/20) — not currently
 * rendered on any live page (see types/testimonial.ts for why). Kept as
 * ready-to-use architecture so a Testimonials section can be added quickly
 * once the business confirms real reviews, without inventing UI at that
 * point under time pressure.
 */
function TestimonialCard({ testimonial }: TestimonialCardProps) {
  return (
    <article className="content-card">
      <p className="content-card__desc" style={{ fontStyle: "italic" }}>&ldquo;{testimonial.text}&rdquo;</p>
      <p className="content-card__meta">
        <span aria-label={`${testimonial.rating} out of 5 stars`}>{"★".repeat(testimonial.rating)}</span>
      </p>
      <p className="content-card__title" style={{ textTransform: "none" }}>{testimonial.name}</p>
      <p className="text-muted">{testimonial.role}</p>
    </article>
  );
}

export default TestimonialCard;
