import { apiRequest } from "./apiClient";
import type { AdminAlertsSummaryDto } from "../types/adminAlerts";

export function getAdminAlertsSummary(accessToken: string): Promise<AdminAlertsSummaryDto> {
  return apiRequest<AdminAlertsSummaryDto>("/admin/alerts/summary", { accessToken });
}

export function markBookingsAlertsSeen(accessToken: string): Promise<void> {
  return apiRequest<void>("/admin/alerts/bookings/seen", { method: "POST", accessToken });
}
