import { apiRequest } from "./apiClient";
import type { PublicVehicleDto } from "../types/publicVehicle";

export function getActiveVehicles(): Promise<PublicVehicleDto[]> {
  return apiRequest<PublicVehicleDto[]>("/public/vehicles");
}
