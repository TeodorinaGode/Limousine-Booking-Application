import { useEffect } from "react";
import { APP_BRAND_NAME } from "../config/brand";

function upsertMeta(attr: "name" | "property", key: string, content: string): HTMLMetaElement {
  let tag = document.querySelector<HTMLMetaElement>(`meta[${attr}="${key}"]`);
  if (!tag) {
    tag = document.createElement("meta");
    tag.setAttribute(attr, key);
    document.head.appendChild(tag);
  }
  tag.setAttribute("content", content);
  return tag;
}

/**
 * Sets the document title, meta description, canonical link, and OpenGraph
 * tags for the current page (Prompt 17 section 36, extended for basic SEO
 * coverage) — no routing/SSR framework here, so this is the lightest correct
 * way to keep each public page's head metadata accurate without a new
 * dependency. Every tag it creates is removed/restored on unmount so
 * switching pages never leaves stale metadata behind.
 */
export function usePageMeta(title: string, description?: string) {
  useEffect(() => {
    const previousTitle = document.title;
    document.title = title;

    const ogTitleTag = upsertMeta("property", "og:title", title);
    const ogSiteNameTag = upsertMeta("property", "og:site_name", APP_BRAND_NAME);
    const ogTypeTag = upsertMeta("property", "og:type", "website");
    const canonicalUrl = `${window.location.origin}${window.location.pathname}`;
    const ogUrlTag = upsertMeta("property", "og:url", canonicalUrl);

    let canonicalLink = document.querySelector<HTMLLinkElement>('link[rel="canonical"]');
    const hadCanonicalLink = canonicalLink !== null;
    if (!canonicalLink) {
      canonicalLink = document.createElement("link");
      canonicalLink.setAttribute("rel", "canonical");
      document.head.appendChild(canonicalLink);
    }
    canonicalLink.setAttribute("href", canonicalUrl);

    let descriptionMeta = document.querySelector<HTMLMetaElement>('meta[name="description"]');
    const previousDescription = descriptionMeta?.getAttribute("content") ?? null;
    let ogDescriptionTag: HTMLMetaElement | null = null;
    const previousOgDescription = document.querySelector<HTMLMetaElement>('meta[property="og:description"]')?.getAttribute("content") ?? null;

    if (description) {
      if (!descriptionMeta) {
        descriptionMeta = document.createElement("meta");
        descriptionMeta.setAttribute("name", "description");
        document.head.appendChild(descriptionMeta);
      }
      descriptionMeta.setAttribute("content", description);
      ogDescriptionTag = upsertMeta("property", "og:description", description);
    }

    return () => {
      document.title = previousTitle;
      ogTitleTag.remove();
      ogSiteNameTag.remove();
      ogTypeTag.remove();
      ogUrlTag.remove();
      if (!hadCanonicalLink) canonicalLink?.remove();
      if (description && descriptionMeta) {
        if (previousDescription === null) descriptionMeta.remove();
        else descriptionMeta.setAttribute("content", previousDescription);
      }
      if (ogDescriptionTag) {
        if (previousOgDescription === null) ogDescriptionTag.remove();
        else ogDescriptionTag.setAttribute("content", previousOgDescription);
      }
    };
  }, [title, description]);
}
