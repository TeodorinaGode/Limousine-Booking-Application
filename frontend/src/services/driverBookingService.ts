import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type { DriverDto } from "../types/driver";
import type {
  DriverBookingDetailDto,
  DriverBookingListItemDto,
  DriverBookingSearchParams,
  DriverDashboardDto,
} from "../types/driverBooking";

export function getMyDashboard(accessToken: string): Promise<DriverDashboardDto> {
  return apiRequest<DriverDashboardDto>("/driver/dashboard", { accessToken });
}

export function getMyProfile(accessToken: string): Promise<DriverDto> {
  return apiRequest<DriverDto>("/driver/profile", { accessToken });
}

export function getMyBookings(
  params: DriverBookingSearchParams,
  accessToken: string
): Promise<PagedResult<DriverBookingListItemDto>> {
  return apiRequest<PagedResult<DriverBookingListItemDto>>("/driver/bookings", {
    accessToken,
    query: {
      dateFrom: params.dateFrom,
      dateTo: params.dateTo,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
}

export function getMyBookingById(id: string, accessToken: string): Promise<DriverBookingDetailDto> {
  return apiRequest<DriverBookingDetailDto>(`/driver/bookings/${id}`, { accessToken });
}

export function startRide(id: string, accessToken: string): Promise<DriverBookingDetailDto> {
  return apiRequest<DriverBookingDetailDto>(`/driver/bookings/${id}/start`, { method: "POST", accessToken });
}

export function markPassengerPickedUp(id: string, accessToken: string): Promise<DriverBookingDetailDto> {
  return apiRequest<DriverBookingDetailDto>(`/driver/bookings/${id}/pickup`, { method: "POST", accessToken });
}

export function completeRide(id: string, accessToken: string): Promise<DriverBookingDetailDto> {
  return apiRequest<DriverBookingDetailDto>(`/driver/bookings/${id}/complete`, { method: "POST", accessToken });
}
