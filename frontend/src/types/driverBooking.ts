import type { BookingRouteSummary } from "./adminBooking";

export interface DriverBookingListItemDto {
  id: string;
  bookingReference: string;
  route: BookingRouteSummary;
  bookingDate: string;
  pickupTime: string;
  pickupAddress: string;
  passengerCount: number;
  customerFirstName: string;
  customerLastName: string;
  status: string;
  /** Upcoming | OnTheWay | PassengerPickedUp | Completed | Cancelled */
  rideStatus: string;
}

export interface RideStatusHistoryItemDto {
  previousStatus: string;
  newStatus: string;
  changedAt: string;
}

export interface DriverBookingDetailDto {
  id: string;
  bookingReference: string;
  customerFirstName: string;
  customerLastName: string;
  customerPhone: string;
  route: BookingRouteSummary;
  bookingDate: string;
  pickupTime: string;
  estimatedDurationMinutes: number;
  estimatedEndTime: string;
  pickupAddress: string;
  passengerCount: number;
  notes: string | null;
  status: string;
  rideStatus: string;
  rideStatusHistory: RideStatusHistoryItemDto[];
}

export interface DriverDashboardDto {
  today: string;
  isAvailable: boolean;
  todaysTripCount: number;
  completedTodayCount: number;
  upcomingTripCount: number;
  todaysTrips: DriverBookingListItemDto[];
  nextTrip: DriverBookingListItemDto | null;
}

export interface DriverBookingSearchParams {
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}
