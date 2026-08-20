export type PublicLocationType = "City" | "Airport" | "Destination";

export interface PublicLocationDto {
  id: string;
  name: string;
  countryCode: string;
  latitude: number;
  longitude: number;
  type: PublicLocationType;
  description: string | null;
}

export interface PublicLocationsDto {
  enabled: boolean;
  provider: string;
  defaultLatitude: number;
  defaultLongitude: number;
  defaultZoom: number;
  locations: PublicLocationDto[];
}
