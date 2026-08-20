import { apiRequest } from "./apiClient";
import type { CompanyInfoDto } from "../types/company";

export function getCompanyInfo(): Promise<CompanyInfoDto> {
  return apiRequest<CompanyInfoDto>("/public/company");
}
