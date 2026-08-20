import { apiRequest, ApiError } from "./apiClient";
import type {
  AssignmentReportDto,
  BookingStatusDistributionDto,
  BookingsByDayDto,
  CancellationReportDto,
  DriverActivityDto,
  PassengerReportDto,
  PaymentReportDto,
  PopularRouteDto,
  ReportDateRangeParams,
  ReportSummaryDto,
  RevenueByDayDto,
  UnassignedBookingDto,
  UpcomingOperationDto,
  VehicleUsageDto,
} from "../types/reports";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000/api";

export function getSummary(params: ReportDateRangeParams, accessToken: string): Promise<ReportSummaryDto> {
  return apiRequest<ReportSummaryDto>("/admin/reports/summary", { accessToken, query: params });
}

export function getRevenueByDay(params: ReportDateRangeParams, accessToken: string): Promise<RevenueByDayDto[]> {
  return apiRequest<RevenueByDayDto[]>("/admin/reports/revenue-by-day", { accessToken, query: params });
}

export function getBookingsByDay(params: ReportDateRangeParams, accessToken: string): Promise<BookingsByDayDto[]> {
  return apiRequest<BookingsByDayDto[]>("/admin/reports/bookings-by-day", { accessToken, query: params });
}

export function getPopularRoutes(
  params: ReportDateRangeParams & { top?: number },
  accessToken: string
): Promise<PopularRouteDto[]> {
  return apiRequest<PopularRouteDto[]>("/admin/reports/routes", { accessToken, query: params });
}

export function getDriverActivity(params: ReportDateRangeParams, accessToken: string): Promise<DriverActivityDto[]> {
  return apiRequest<DriverActivityDto[]>("/admin/reports/drivers", { accessToken, query: params });
}

export function getVehicleUsage(params: ReportDateRangeParams, accessToken: string): Promise<VehicleUsageDto[]> {
  return apiRequest<VehicleUsageDto[]>("/admin/reports/vehicles", { accessToken, query: params });
}

export function getPassengerReport(params: ReportDateRangeParams, accessToken: string): Promise<PassengerReportDto> {
  return apiRequest<PassengerReportDto>("/admin/reports/passengers", { accessToken, query: params });
}

export function getStatusDistribution(
  params: ReportDateRangeParams,
  accessToken: string
): Promise<BookingStatusDistributionDto[]> {
  return apiRequest<BookingStatusDistributionDto[]>("/admin/reports/status-distribution", { accessToken, query: params });
}

export function getAssignmentReport(params: ReportDateRangeParams, accessToken: string): Promise<AssignmentReportDto> {
  return apiRequest<AssignmentReportDto>("/admin/reports/assignments", { accessToken, query: params });
}

export function getPaymentReport(params: ReportDateRangeParams, accessToken: string): Promise<PaymentReportDto> {
  return apiRequest<PaymentReportDto>("/admin/reports/payments", { accessToken, query: params });
}

export function getUnassignedBookings(
  page: number,
  pageSize: number,
  accessToken: string
): Promise<UnassignedBookingDto[]> {
  return apiRequest<UnassignedBookingDto[]>("/admin/reports/unassigned", { accessToken, query: { page, pageSize } });
}

export function getUpcomingOperations(period: string, accessToken: string): Promise<UpcomingOperationDto[]> {
  return apiRequest<UpcomingOperationDto[]>("/admin/reports/upcoming", { accessToken, query: { period } });
}

export function getCancellationReport(params: ReportDateRangeParams, accessToken: string): Promise<CancellationReportDto> {
  return apiRequest<CancellationReportDto>("/admin/reports/cancellations", { accessToken, query: params });
}

/** Downloads a CSV export by triggering the browser's native save flow — never re-serializes an already-loaded paginated page (section 32). */
async function downloadCsv(
  path: string,
  params: Record<string, string | number | boolean | undefined>,
  accessToken: string,
  fileName: string
): Promise<void> {
  const url = new URL(`${API_BASE_URL}${path}`, window.location.origin);
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== "") url.searchParams.set(key, String(value));
  }

  const response = await fetch(url.toString(), { headers: { Authorization: `Bearer ${accessToken}` } });
  if (!response.ok) {
    throw new ApiError(response.status, `Failed to export ${fileName} (status ${response.status}).`);
  }

  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = objectUrl;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(objectUrl);
}

export function exportBookingsCsv(params: ReportDateRangeParams, accessToken: string): Promise<void> {
  return downloadCsv("/admin/reports/bookings/export", params, accessToken, "bookings-report.csv");
}

export function exportRoutesCsv(params: ReportDateRangeParams & { top?: number }, accessToken: string): Promise<void> {
  return downloadCsv("/admin/reports/routes/export", params, accessToken, "routes-report.csv");
}

export function exportDriversCsv(params: ReportDateRangeParams, accessToken: string): Promise<void> {
  return downloadCsv("/admin/reports/drivers/export", params, accessToken, "drivers-report.csv");
}

export function exportVehiclesCsv(params: ReportDateRangeParams, accessToken: string): Promise<void> {
  return downloadCsv("/admin/reports/vehicles/export", params, accessToken, "vehicles-report.csv");
}
