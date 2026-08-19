export interface DriverVehicleSummary {
  id: string;
  registrationNumber: string;
  make: string;
  model: string;
}

export interface DriverDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  isActive: boolean;
  isAvailable: boolean;
  vehicle: DriverVehicleSummary | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateDriverRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  password: string;
  vehicleId?: string | null;
}

export interface UpdateDriverRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  isActive: boolean;
  vehicleId?: string | null;
}

export interface ResetDriverPasswordRequest {
  newPassword: string;
}

export type ActiveFilter = "all" | "active" | "inactive";
export type AvailabilityFilter = "all" | "available" | "unavailable";
export type VehicleFilter = "all" | "assigned" | "unassigned";

export interface DriverSearchParams {
  search?: string;
  isActive?: boolean;
  isAvailable?: boolean;
  hasVehicle?: boolean;
  page?: number;
  pageSize?: number;
}
