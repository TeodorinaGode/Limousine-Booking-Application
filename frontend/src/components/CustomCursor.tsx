import { useEffect, useRef, useState } from "react";

/**
 * Subtle decorative cursor companion (Prompt 21, section 29) — desktop only
 * (`(hover: hover) and (pointer: fine)`, disabled on touch per the spec's
 * explicit instruction), and respects `prefers-reduced-motion` by rendering
 * nothing at all rather than animating.
 *
 * Deliberately ADDITIVE, not a replacement for the native cursor: hiding the
 * OS cursor site-wide would work against section 38's "do not sacrifice
 * accessibility for aesthetics" for anyone relying on native cursor
 * feedback (motor impairments, screen magnifiers). This dot follows
 * alongside the real cursor instead of hiding it.
 *
 * Hoverable elements opt in with `data-cursor="view"` or `data-cursor="explore"`
 * to swap the dot for a small text label; everything else just gets a
 * slightly larger plain dot on hover via `a`/`button`.
 */
function CustomCursor() {
  const [isEnabled, setIsEnabled] = useState(false);
  const [label, setLabel] = useState<string | null>(null);
  const [isOverInteractive, setIsOverInteractive] = useState(false);
  const dotRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (typeof window.matchMedia !== "function") return;

    const supportsFinePointer = window.matchMedia("(hover: hover) and (pointer: fine)").matches;
    const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (!supportsFinePointer || prefersReducedMotion) return;

    setIsEnabled(true);

    const handleMove = (event: MouseEvent) => {
      const node = dotRef.current;
      if (node) {
        node.style.transform = `translate3d(${event.clientX}px, ${event.clientY}px, 0)`;
      }

      const target = (event.target as HTMLElement)?.closest?.("[data-cursor], a, button");
      if (!target) {
        setIsOverInteractive(false);
        setLabel(null);
        return;
      }

      setIsOverInteractive(true);
      setLabel(target.getAttribute("data-cursor"));
    };

    window.addEventListener("mousemove", handleMove, { passive: true });
    return () => window.removeEventListener("mousemove", handleMove);
  }, []);

  if (!isEnabled) return null;

  return (
    <div
      ref={dotRef}
      className={`noir-cursor${isOverInteractive ? " noir-cursor--link" : ""}${label ? " noir-cursor--label" : ""}`}
      aria-hidden="true"
    >
      {label}
    </div>
  );
}

export default CustomCursor;
