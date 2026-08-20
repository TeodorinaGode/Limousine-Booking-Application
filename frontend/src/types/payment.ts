export interface PaymentCheckoutDto {
  paymentId: string;
  checkoutUrl: string;
  expiresAt: string;
}

/** Pending | Processing | Paid | Failed | Cancelled | Refunded */
export interface PublicPaymentStatusDto {
  bookingReference: string;
  status: string;
  amount: number;
  currency: string;
  paidAt: string | null;
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
