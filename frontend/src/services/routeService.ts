import { apiRequest } from "./apiClient";
import type { PagedResult } from "../types/api";
import type { CreateRouteRequest, RouteDto, RouteSearchParams, UpdateRouteRequest } from "../types/route";

export function getRoutes(params: RouteSearchParams, accessToken: string): Promise<PagedResult<RouteDto>> {
  return apiRequest<PagedResult<RouteDto>>("/admin/routes", {
    accessToken,
    query: {
      search: params.search,
      isActive: params.isActive,
      page: params.page,
      pageSize: params.pageSize,
    },
  });
}

export function getRouteById(id: string, accessToken: string): Promise<RouteDto> {
  return apiRequest<RouteDto>(`/admin/routes/${id}`, { accessToken });
}

export function createRoute(data: CreateRouteRequest, accessToken: string): Promise<RouteDto> {
  return apiRequest<RouteDto>("/admin/routes", { method: "POST", body: data, accessToken });
}

export function updateRoute(id: string, data: UpdateRouteRequest, accessToken: string): Promise<RouteDto> {
  return apiRequest<RouteDto>(`/admin/routes/${id}`, { method: "PUT", body: data, accessToken });
}

export function activateRoute(id: string, accessToken: string): Promise<RouteDto> {
  return apiRequest<RouteDto>(`/admin/routes/${id}/activate`, { method: "PUT", accessToken });
}

export function deactivateRoute(id: string, accessToken: string): Promise<RouteDto> {
  return apiRequest<RouteDto>(`/admin/routes/${id}/deactivate`, { method: "PUT", accessToken });
}
