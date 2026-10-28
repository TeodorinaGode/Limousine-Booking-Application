import { apiRequest } from "./apiClient";
import type { CrmDashboardDto, CrmReportSummaryDto } from "../types/crmAnalytics";

export function getCrmDashboard(accessToken: string): Promise<CrmDashboardDto> {
  return apiRequest<CrmDashboardDto>("/admin/crm/dashboard", { accessToken });
}

export function getCrmReportSummary(
  params: { dateFrom: string; dateTo: string },
  accessToken: string
): Promise<CrmReportSummaryDto> {
  return apiRequest<CrmReportSummaryDto>("/admin/crm/reports/summary", {
    accessToken,
    query: { dateFrom: params.dateFrom, dateTo: params.dateTo },
  });
}
