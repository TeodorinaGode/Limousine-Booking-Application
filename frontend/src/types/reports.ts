import type { BookingRouteSummary } from "./adminBooking";

export interface ReportDateRangeParams {
  dateFrom?: string;
  dateTo?: string;
  [key: string]: string | number | boolean | undefined;
}

export interface ReportSummaryDto {
  dateFrom: string;
  dateTo: string;
  totalBookings: number;
  confirmedBookings: number;
  pendingBookings: number;
  completedBookings: number;
  cancelledBookings: number;
  grossRevenue: number;
  completedRevenue: number;
  averageBookingValue: number;
  averageCompletedBookingValue: number;
  manualAssignments: number;
  automaticAssignments: number;
  currency: string;
}

export interface RevenueByDayDto {
  date: string;
  bookingCount: number;
  revenue: number;
}

export interface BookingsByDayDto {
  date: string;
  total: number;
  completed: number;
  cancelled: number;
  pending: number;
  confirmed: number;
}

export interface PopularRouteDto {
  routeId: string;
  departureLocation: string;
  destination: string;
  bookingCount: number;
  revenue: number;
  percentageOfTotalBookings: number;
}

export interface DriverActivityDto {
  driverId: string;
  driverName: string;
  assignedBookings: number;
  completedRides: number;
  cancelledBookings: number;
  upcomingBookings: number;
  manualAssignments: number;
  completionRate: number;
}

export interface VehicleUsageDto {
  vehicleId: string;
  vehicleDescription: string;
  assignedBookings: number;
  completedRides: number;
  upcomingBookings: number;
  totalPassengers: number;
  utilization: number;
}

export interface PassengerReportDto {
  dateFrom: string;
  dateTo: string;
  totalPassengers: number;
  averagePassengersPerBooking: number;
  maximumPassengersInABooking: number;
}

export interface BookingStatusDistributionDto {
  status: string;
  count: number;
  percentage: number;
}

export interface AssignmentReportDto {
  dateFrom: string;
  dateTo: string;
  automaticAssignments: number;
  manualAssignments: number;
  requiresManualAssignment: number;
  manualAssignmentRate: number;
  assignmentSuccessRate: number;
}

export interface UnassignedBookingDto {
  id: string;
  bookingReference: string;
  bookingDate: string;
  pickupTime: string;
  route: BookingRouteSummary;
  customerFirstName: string;
  customerLastName: string;
  passengerCount: number;
  reason: string | null;
  createdAt: string;
}

export interface UpcomingOperationDto {
  id: string;
  bookingReference: string;
  bookingDate: string;
  pickupTime: string;
  route: BookingRouteSummary;
  customerFirstName: string;
  customerLastName: string;
  driverName: string | null;
  vehicleDescription: string | null;
  status: string;
  rideStatus: string;
}

export interface CancellationsByRouteDto {
  routeId: string;
  departureLocation: string;
  destination: string;
  count: number;
}

export interface CancellationsByDayDto {
  date: string;
  count: number;
}

export interface CancellationReasonDto {
  reason: string;
  count: number;
}

export interface CancellationReportDto {
  dateFrom: string;
  dateTo: string;
  totalCancellations: number;
  totalBookings: number;
  cancellationRate: number;
  cancellationsByRoute: CancellationsByRouteDto[];
  cancellationsByDay: CancellationsByDayDto[];
  cancellationsByReason: CancellationReasonDto[];
}
