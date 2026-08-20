import { useEffect } from "react";

/** Sets the document title and meta description for the current page (Prompt 17, section 36) — no routing/SSR framework here, so this is the lightest correct way to keep each public page's `<title>`/description accurate without a new dependency. */
export function usePageMeta(title: string, description?: string) {
  useEffect(() => {
    const previousTitle = document.title;
    document.title = title;

    let meta = document.querySelector<HTMLMetaElement>('meta[name="description"]');
    const previousDescription = meta?.getAttribute("content") ?? null;

    if (description) {
      if (!meta) {
        meta = document.createElement("meta");
        meta.setAttribute("name", "description");
        document.head.appendChild(meta);
      }
      meta.setAttribute("content", description);
    }

    return () => {
      document.title = previousTitle;
      if (description && meta) {
        if (previousDescription === null) meta.remove();
        else meta.setAttribute("content", previousDescription);
      }
    };
  }, [title, description]);
}
