export interface VehicleDto {
  id: string;
  registrationNumber: string;
  make: string;
  model: string;
  vehicleType: string;
  passengerCapacity: number;
  isActive: boolean;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateVehicleRequest {
  registrationNumber: string;
  make: string;
  model: string;
  vehicleType: string;
  passengerCapacity: number;
  notes?: string | null;
}

export interface UpdateVehicleRequest extends CreateVehicleRequest {
  isActive: boolean;
}

export type ActiveFilter = "all" | "active" | "inactive";

export interface VehicleSearchParams {
  search?: string;
  isActive?: boolean;
  minCapacity?: number;
  page?: number;
  pageSize?: number;
}
