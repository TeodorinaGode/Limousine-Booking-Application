import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type {
  CreateDriverRequest,
  DriverDto,
  DriverSearchParams,
  ResetDriverPasswordRequest,
  UpdateDriverRequest,
} from "../types/driver";

export function getDrivers(params: DriverSearchParams, accessToken: string): Promise<PagedResult<DriverDto>> {
  return apiRequest<PagedResult<DriverDto>>("/admin/drivers", {
    accessToken,
    query: {
      search: params.search,
      isActive: params.isActive,
      isAvailable: params.isAvailable,
      hasVehicle: params.hasVehicle,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
}

export function getDriverById(id: string, accessToken: string): Promise<DriverDto> {
  return apiRequest<DriverDto>(`/admin/drivers/${id}`, { accessToken });
}

export function createDriver(data: CreateDriverRequest, accessToken: string): Promise<DriverDto> {
  return apiRequest<DriverDto>("/admin/drivers", { method: "POST", body: data, accessToken });
}

export function updateDriver(id: string, data: UpdateDriverRequest, accessToken: string): Promise<DriverDto> {
  return apiRequest<DriverDto>(`/admin/drivers/${id}`, { method: "PUT", body: data, accessToken });
}

export function activateDriver(id: string, accessToken: string): Promise<DriverDto> {
  return apiRequest<DriverDto>(`/admin/drivers/${id}/activate`, { method: "PUT", accessToken });
}

export function deactivateDriver(id: string, accessToken: string): Promise<DriverDto> {
  return apiRequest<DriverDto>(`/admin/drivers/${id}/deactivate`, { method: "PUT", accessToken });
}

export function resetDriverPassword(
  id: string,
  data: ResetDriverPasswordRequest,
  accessToken: string
): Promise<DriverDto> {
  return apiRequest<DriverDto>(`/admin/drivers/${id}/password`, { method: "PUT", body: data, accessToken });
}
