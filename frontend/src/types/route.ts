export interface RouteDto {
  id: string;
  departureLocation: string;
  destination: string;
  estimatedDurationMinutes: number;
  price: number;
  currency: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateRouteRequest {
  departureLocation: string;
  destination: string;
  estimatedDurationMinutes: number;
  price: number;
  currency: string;
}

export interface UpdateRouteRequest extends CreateRouteRequest {
  isActive: boolean;
}

export type ActiveFilter = "all" | "active" | "inactive";

export interface RouteSearchParams {
  search?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}
