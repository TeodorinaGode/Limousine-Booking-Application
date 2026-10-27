import { useCallback, useEffect, useState } from "react";

/**
 * Fades/slides a section in the first time it enters the viewport (site-wide
 * "modern" layout pass). Falls back to immediately visible when
 * `IntersectionObserver` isn't available (e.g. under jsdom in tests), so
 * component tests never have to deal with hidden content. Stops observing
 * after the first reveal — this is a one-time entrance effect, not a
 * repeating scroll animation.
 *
 * Uses a callback ref (not `useRef` + an effect with an empty dependency
 * array) deliberately — several call sites attach this `ref` to a section
 * that's behind a conditional render gated on async-loaded data (e.g. "only
 * render once `locationsData`/`company` has loaded"). With a plain ref, the
 * one-time mount effect reads `ref.current` as null before that data ever
 * arrives, bails out, and never retries — the section stays permanently at
 * opacity: 0 no matter how long you wait or scroll. A callback ref re-fires
 * (and the effect below, keyed on the resulting state, re-runs) whenever
 * React actually attaches the node, however late that happens.
 */
export function useScrollReveal<T extends HTMLElement = HTMLDivElement>() {
  const [node, setNode] = useState<T | null>(null);
  const [isVisible, setIsVisible] = useState(false);

  const ref = useCallback((element: T | null) => {
    setNode(element);
  }, []);

  useEffect(() => {
    if (!node) return;

    if (typeof IntersectionObserver === "undefined") {
      setIsVisible(true);
      return;
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setIsVisible(true);
          observer.disconnect();
        }
      },
      { threshold: 0.15 }
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, [node]);

  return { ref, className: isVisible ? "reveal reveal--visible" : "reveal" };
}
