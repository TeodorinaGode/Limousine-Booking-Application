import { act, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { useScrollReveal } from "./useScrollReveal";

function Probe() {
  const reveal = useScrollReveal<HTMLDivElement>();
  return (
    <div ref={reveal.ref} data-testid="probe" className={reveal.className}>
      content
    </div>
  );
}

const originalIntersectionObserver = globalThis.IntersectionObserver;

afterEach(() => {
  globalThis.IntersectionObserver = originalIntersectionObserver;
  vi.restoreAllMocks();
});

describe("useScrollReveal", () => {
  it("is immediately visible when IntersectionObserver is unavailable, rather than staying hidden forever", () => {
    // @ts-expect-error simulating an environment without IntersectionObserver
    delete globalThis.IntersectionObserver;

    render(<Probe />);

    expect(screen.getByTestId("probe")).toHaveClass("reveal", "reveal--visible");
  });

  it("starts hidden and becomes visible once the observer reports intersection", () => {
    let capturedCallback: IntersectionObserverCallback | null = null;
    const disconnect = vi.fn();
    const observe = vi.fn();

    class FakeIntersectionObserver {
      constructor(callback: IntersectionObserverCallback) {
        capturedCallback = callback;
      }
      observe = observe;
      disconnect = disconnect;
      unobserve = vi.fn();
      takeRecords = vi.fn(() => []);
      root = null;
      rootMargin = "";
      thresholds: number[] = [];
    }

    globalThis.IntersectionObserver = FakeIntersectionObserver;

    render(<Probe />);

    expect(screen.getByTestId("probe")).toHaveClass("reveal");
    expect(screen.getByTestId("probe")).not.toHaveClass("reveal--visible");
    expect(observe).toHaveBeenCalledTimes(1);

    act(() => {
      capturedCallback!([{ isIntersecting: true } as IntersectionObserverEntry], new FakeIntersectionObserver(() => undefined) as unknown as IntersectionObserver);
    });

    expect(screen.getByTestId("probe")).toHaveClass("reveal", "reveal--visible");
    expect(disconnect).toHaveBeenCalled();
  });
});
