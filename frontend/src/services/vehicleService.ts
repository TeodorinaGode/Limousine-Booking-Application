import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type { CreateVehicleRequest, UpdateVehicleRequest, VehicleDto, VehicleSearchParams } from "../types/vehicle";

export function getVehicles(params: VehicleSearchParams, accessToken: string): Promise<PagedResult<VehicleDto>> {
  return apiRequest<PagedResult<VehicleDto>>("/admin/vehicles", {
    accessToken,
    query: {
      search: params.search,
      isActive: params.isActive,
      minCapacity: params.minCapacity,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
}

export function getVehicleById(id: string, accessToken: string): Promise<VehicleDto> {
  return apiRequest<VehicleDto>(`/admin/vehicles/${id}`, { accessToken });
}

export function createVehicle(data: CreateVehicleRequest, accessToken: string): Promise<VehicleDto> {
  return apiRequest<VehicleDto>("/admin/vehicles", { method: "POST", body: data, accessToken });
}

export function updateVehicle(id: string, data: UpdateVehicleRequest, accessToken: string): Promise<VehicleDto> {
  return apiRequest<VehicleDto>(`/admin/vehicles/${id}`, { method: "PUT", body: data, accessToken });
}

export function activateVehicle(id: string, accessToken: string): Promise<VehicleDto> {
  return apiRequest<VehicleDto>(`/admin/vehicles/${id}/activate`, { method: "PUT", accessToken });
}

export function deactivateVehicle(id: string, accessToken: string): Promise<VehicleDto> {
  return apiRequest<VehicleDto>(`/admin/vehicles/${id}/deactivate`, { method: "PUT", accessToken });
}
