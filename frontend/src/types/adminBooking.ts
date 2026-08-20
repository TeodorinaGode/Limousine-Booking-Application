export interface BookingRouteSummary {
  departureLocation: string;
  destination: string;
}

export interface AdminBookingListItemDto {
  id: string;
  bookingReference: string;
  customerFirstName: string;
  customerLastName: string;
  route: BookingRouteSummary;
  bookingDate: string;
  pickupTime: string;
  passengerCount: number;
  price: number;
  currency: string;
  status: string;
  /** Upcoming | OnTheWay | PassengerPickedUp | Completed | Cancelled — view-only; only the driver's own endpoints can change it. */
  rideStatus: string;
  driverName: string | null;
  vehicleDescription: string | null;
  /** "Automatic" | "Manual" | "Unassigned" */
  assignment: string;
  /** "NotStarted" if no payment attempt exists yet, otherwise the most recent attempt's status. */
  paymentStatus: string;
}

export interface AssignmentHistoryItemDto {
  driverName: string;
  vehicleDescription: string;
  assignmentType: string;
  assignedByEmail: string | null;
  assignedAt: string;
}

export interface RideStatusHistoryEntryDto {
  previousStatus: string;
  newStatus: string;
  changedAt: string;
}

export interface AdminPaymentSummaryDto {
  status: string;
  amount: number;
  currency: string;
  provider: string;
  paidAt: string | null;
}

export interface AdminPaymentHistoryItemDto {
  status: string;
  amount: number;
  currency: string;
  createdAt: string;
  paidAt: string | null;
  failureReason: string | null;
}

export interface AdminBookingDetailDto {
  id: string;
  bookingReference: string;
  customerFirstName: string;
  customerLastName: string;
  customerEmail: string;
  customerPhone: string;
  routeId: string;
  route: BookingRouteSummary;
  bookingDate: string;
  pickupTime: string;
  estimatedDurationMinutes: number;
  estimatedEndTime: string;
  pickupAddress: string;
  passengerCount: number;
  notes: string | null;
  price: number;
  currency: string;
  status: string;
  /** Upcoming | OnTheWay | PassengerPickedUp | Completed | Cancelled — view-only; only the driver's own endpoints can change it. */
  rideStatus: string;
  rideStatusHistory: RideStatusHistoryEntryDto[];
  driverId: string | null;
  driverName: string | null;
  vehicleId: string | null;
  vehicleDescription: string | null;
  assignmentType: string | null;
  requiresManualAssignment: boolean;
  manualAssignmentReason: string | null;
  cancellationReason: string | null;
  cancelledAt: string | null;
  cancelledByEmail: string | null;
  createdAt: string;
  updatedAt: string;
  assignmentHistory: AssignmentHistoryItemDto[];
  /** The most recent payment attempt, or null if payment was never started. */
  payment: AdminPaymentSummaryDto | null;
  /** Every payment attempt, most recent first. */
  paymentHistory: AdminPaymentHistoryItemDto[];
}

export interface AdminBookingSearchParams {
  search?: string;
  /** Comma-separated BookingStatus names, e.g. "Pending,Confirmed". */
  status?: string;
  dateFrom?: string;
  dateTo?: string;
  driverId?: string;
  vehicleId?: string;
  routeId?: string;
  /** all | automatic | manual | requiresManual */
  assignmentFilter?: string;
  /** all | notStarted | pending | processing | paid | failed | cancelled | refunded */
  paymentStatus?: string;
  sortBy?: string;
  sortDirection?: string;
  page?: number;
  pageSize?: number;
}

export interface UpdateBookingRequest {
  routeId: string;
  bookingDate: string;
  pickupTime: string;
  pickupAddress: string;
  passengerCount: number;
  customerFirstName: string;
  customerLastName: string;
  customerEmail: string;
  customerPhone: string;
  notes?: string;
}

export interface AssignDriverRequest {
  driverId: string;
  vehicleId: string;
}

export interface CancelBookingRequest {
  reason?: string;
}

export interface UpcomingBookingItemDto {
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
}

export interface NotificationSummaryDto {
  pending: number;
  retrying: number;
  failed: number;
  sentToday: number;
}

export interface AdminDashboardDto {
  totalBookings: number;
  todaysBookings: number;
  pendingBookings: number;
  requiresManualAssignmentCount: number;
  confirmedBookings: number;
  cancelledBookings: number;
  upcomingTripsCount: number;
  upcomingBookings: UpcomingBookingItemDto[];
  notifications: NotificationSummaryDto;
}
