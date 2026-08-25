import { useEffect, useRef, useState } from "react";

/**
 * Fades/slides a section in the first time it enters the viewport (site-wide
 * "modern" layout pass). Falls back to immediately visible when
 * `IntersectionObserver` isn't available (e.g. under jsdom in tests), so
 * component tests never have to deal with hidden content. Stops observing
 * after the first reveal — this is a one-time entrance effect, not a
 * repeating scroll animation.
 */
export function useScrollReveal<T extends HTMLElement = HTMLDivElement>() {
  const ref = useRef<T>(null);
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    const node = ref.current;
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
  }, []);

  return { ref, className: isVisible ? "reveal reveal--visible" : "reveal" };
}
