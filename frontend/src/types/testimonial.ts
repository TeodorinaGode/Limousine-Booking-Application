/**
 * Content-model shape for a customer testimonial (Prompt 18, section 18/20).
 * Not backed by a database table or any API endpoint yet — the old
 * roi-limousinen.ch site has two named testimonials, but they could not be
 * verified as real, business-confirmed reviews (no channel to ask the
 * business), so per the spec's explicit "do not fabricate customer reviews"
 * instruction, this type and TestimonialCard exist only as the prepared
 * architecture for a future Testimonials section — no testimonial content is
 * wired into any live page. See the README's Prompt 18 "requires business
 * confirmation" list.
 */
export interface Testimonial {
  name: string;
  role: string;
  text: string;
  rating: number;
  active: boolean;
}
