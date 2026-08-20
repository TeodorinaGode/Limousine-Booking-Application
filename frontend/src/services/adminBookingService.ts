import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type {
  AdminBookingDetailDto,
  AdminBookingListItemDto,
  AdminBookingSearchParams,
  AdminDashboardDto,
  AssignDriverRequest,
  CancelBookingRequest,
  UpdateBookingRequest,
} from "../types/adminBooking";

export function getBookings(params: AdminBookingSearchParams, accessToken: string): Promise<PagedResult<AdminBookingListItemDto>> {
  return apiRequest<PagedResult<AdminBookingListItemDto>>("/admin/bookings", {
    accessToken,
    query: {
      search: params.search,
      status: params.status,
      dateFrom: params.dateFrom,
      dateTo: params.dateTo,
      driverId: params.driverId,
      vehicleId: params.vehicleId,
      routeId: params.routeId,
      assignmentFilter: params.assignmentFilter,
      sortBy: params.sortBy,
      sortDirection: params.sortDirection,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
}

export function getBookingById(id: string, accessToken: string): Promise<AdminBookingDetailDto> {
  return apiRequest<AdminBookingDetailDto>(`/admin/bookings/${id}`, { accessToken });
}

export function updateBooking(id: string, data: UpdateBookingRequest, accessToken: string): Promise<AdminBookingDetailDto> {
  return apiRequest<AdminBookingDetailDto>(`/admin/bookings/${id}`, { method: "PUT", body: data, accessToken });
}

export function assignDriver(id: string, data: AssignDriverRequest, accessToken: string): Promise<AdminBookingDetailDto> {
  return apiRequest<AdminBookingDetailDto>(`/admin/bookings/${id}/assign`, { method: "POST", body: data, accessToken });
}

export function autoAssign(id: string, accessToken: string): Promise<AdminBookingDetailDto> {
  return apiRequest<AdminBookingDetailDto>(`/admin/bookings/${id}/auto-assign`, { method: "POST", accessToken });
}

export function cancelBooking(id: string, data: CancelBookingRequest, accessToken: string): Promise<AdminBookingDetailDto> {
  return apiRequest<AdminBookingDetailDto>(`/admin/bookings/${id}/cancel`, { method: "POST", body: data, accessToken });
}

export function getDashboard(accessToken: string): Promise<AdminDashboardDto> {
  return apiRequest<AdminDashboardDto>("/admin/bookings/dashboard", { accessToken });
}

export function resendConfirmation(id: string, accessToken: string): Promise<AdminBookingDetailDto> {
  return apiRequest<AdminBookingDetailDto>(`/admin/bookings/${id}/notifications/confirmation/resend`, { method: "POST", accessToken });
}
