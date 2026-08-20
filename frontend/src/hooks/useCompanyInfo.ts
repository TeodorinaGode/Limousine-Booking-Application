import { useEffect, useState } from "react";
import { getCompanyInfo } from "../services/companyService";
import type { CompanyInfoDto } from "../types/company";

/** Fetches the public company identity/contact info once and caches it in module scope — every page that needs it (Footer, Contact, hero) shares one request instead of one each. */
let cached: CompanyInfoDto | null = null;
let inFlight: Promise<CompanyInfoDto> | null = null;

export function useCompanyInfo(): CompanyInfoDto | null {
  const [company, setCompany] = useState<CompanyInfoDto | null>(cached);

  useEffect(() => {
    if (cached) {
      setCompany(cached);
      return;
    }

    inFlight ??= getCompanyInfo();
    inFlight
      .then((result) => {
        cached = result;
        setCompany(result);
      })
      .catch(() => {
        // Company info is supplementary display content — a failed fetch just leaves it unset.
      });
  }, []);

  return company;
}
