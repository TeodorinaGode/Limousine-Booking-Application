export interface PublicRouteDto {
  id: string;
  departureLocation: string;
  destination: string;
  estimatedDurationMinutes: number;
  price: number;
  currency: string;
}

export interface CreateBookingRequest {
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

export interface BookingRouteSummary {
  departureLocation: string;
  destination: string;
}

export interface BookingDto {
  id: string;
  bookingReference: string;
  status: string;
  route: BookingRouteSummary;
  bookingDate: string;
  pickupTime: string;
  pickupAddress: string;
  passengerCount: number;
  notes: string | null;
  price: number;
  currency: string;
}
