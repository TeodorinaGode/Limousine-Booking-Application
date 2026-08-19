import { apiRequest } from "./apiClient";
import type { BookingDto, CreateBookingRequest, PublicRouteDto } from "../types/booking";

export function getActiveRoutes(): Promise<PublicRouteDto[]> {
  return apiRequest<PublicRouteDto[]>("/public/routes");
}

export function createBooking(data: CreateBookingRequest): Promise<BookingDto> {
  return apiRequest<BookingDto>("/public/bookings", { method: "POST", body: data });
}
