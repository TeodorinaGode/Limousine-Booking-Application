import { apiRequest } from "./apiClient";
import type {
  AvailabilityDateRange,
  AvailabilityDto,
  CreateAvailabilityRequest,
  DriverScheduleDto,
  UpdateAvailabilityRequest,
} from "../types/availability";

// ---- Driver self-service ----

export function getMySchedule(range: AvailabilityDateRange, accessToken: string): Promise<DriverScheduleDto> {
  return apiRequest<DriverScheduleDto>("/driver/availability", {
    accessToken,
    query: { from: range.from, to: range.to },
  });
}

export function setCurrentAvailability(isAvailable: boolean, accessToken: string): Promise<{ isAvailable: boolean }> {
  return apiRequest<{ isAvailable: boolean }>("/driver/availability", {
    method: "PUT",
    body: { isAvailable },
    accessToken,
  });
}

export function createAvailability(data: CreateAvailabilityRequest, accessToken: string): Promise<AvailabilityDto> {
  return apiRequest<AvailabilityDto>("/driver/availability", { method: "POST", body: data, accessToken });
}

export function updateAvailability(
  id: string,
  data: UpdateAvailabilityRequest,
  accessToken: string
): Promise<AvailabilityDto> {
  return apiRequest<AvailabilityDto>(`/driver/availability/${id}`, { method: "PUT", body: data, accessToken });
}

export function deleteAvailability(id: string, accessToken: string): Promise<void> {
  return apiRequest<void>(`/driver/availability/${id}`, { method: "DELETE", accessToken });
}

// ---- Admin (read-only) ----

export function getDriverSchedule(
  driverId: string,
  range: AvailabilityDateRange,
  accessToken: string
): Promise<DriverScheduleDto> {
  return apiRequest<DriverScheduleDto>(`/admin/drivers/${driverId}/availability`, {
    accessToken,
    query: { from: range.from, to: range.to },
  });
}
