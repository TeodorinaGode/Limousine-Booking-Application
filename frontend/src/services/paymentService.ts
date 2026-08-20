import { apiRequest } from "./apiClient";
import type { PaymentCheckoutDto, PublicPaymentStatusDto } from "../types/payment";

export function createPayment(bookingReference: string, token: string): Promise<PaymentCheckoutDto> {
  return apiRequest<PaymentCheckoutDto>(`/public/bookings/${bookingReference}/payment`, {
    method: "POST",
    query: { token },
  });
}

export function retryPayment(bookingReference: string, token: string): Promise<PaymentCheckoutDto> {
  return apiRequest<PaymentCheckoutDto>(`/public/bookings/${bookingReference}/payment/retry`, {
    method: "POST",
    query: { token },
  });
}

export function getPaymentStatus(bookingReference: string, token: string): Promise<PublicPaymentStatusDto> {
  return apiRequest<PublicPaymentStatusDto>(`/public/bookings/${bookingReference}/payment`, {
    query: { token },
  });
}
