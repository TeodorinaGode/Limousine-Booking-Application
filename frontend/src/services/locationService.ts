import { apiRequest } from "./apiClient";
import type { PublicLocationsDto } from "../types/location";

export function getLocations(): Promise<PublicLocationsDto> {
  return apiRequest<PublicLocationsDto>("/public/locations");
}
