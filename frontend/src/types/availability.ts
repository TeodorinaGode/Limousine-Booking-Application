export interface AvailabilityDto {
  id: string;
  driverId: string;
  date: string; // "2026-09-15"
  startTime: string; // "08:00:00"
  endTime: string; // "17:00:00"
  isAvailable: boolean;
  notes: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface DriverScheduleDto {
  isCurrentlyAvailable: boolean;
  schedule: AvailabilityDto[];
}

export interface CreateAvailabilityRequest {
  date: string;
  startTime: string;
  endTime: string;
  isAvailable: boolean;
  notes?: string | null;
}

export interface UpdateAvailabilityRequest extends CreateAvailabilityRequest {}

export interface AvailabilityDateRange {
  from?: string;
  to?: string;
}
