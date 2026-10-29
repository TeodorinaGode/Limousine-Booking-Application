export interface RecentBookingAlertDto {
  id: string;
  bookingReference: string;
  customerName: string;
  createdAt: string;
  isNew: boolean;
}

export interface UnreadEmailThreadAlertDto {
  id: string;
  subject: string;
  fromName: string | null;
  lastMessageAt: string;
}

export interface AdminAlertsSummaryDto {
  newBookingsCount: number;
  recentBookings: RecentBookingAlertDto[];
  unreadEmailsCount: number;
  recentUnreadEmailThreads: UnreadEmailThreadAlertDto[];
}
