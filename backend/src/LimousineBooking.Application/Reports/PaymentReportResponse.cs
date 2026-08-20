namespace LimousineBooking.Application.Reports;

/// <summary>
/// GET /api/admin/reports/payments. Every count/amount is scoped to payment
/// attempts CREATED in the date range (same anchor convention as
/// ReportSummaryResponse.GrossRevenue) — a payment that started before the
/// range but succeeded inside it is not double-counted elsewhere, since it
/// belongs to exactly one CreatedAt bucket. PaidRevenue and RefundedAmount are
/// kept fully separate from ReportSummaryResponse's booking-price-based
/// GrossRevenue/CompletedRevenue — they measure money actually captured or
/// returned by the payment provider, never a booking's price snapshot.
/// </summary>
public class PaymentReportResponse
{
    public DateOnly DateFrom { get; set; }
    public DateOnly DateTo { get; set; }

    public int TotalPaymentAttempts { get; set; }
    public int SuccessfulPayments { get; set; }
    public int FailedPayments { get; set; }
    public int PendingPayments { get; set; }
    public int CancelledPayments { get; set; }
    public int RefundedPayments { get; set; }

    /// <summary>Sum of Payment.Amount for attempts currently Paid — excludes any that were later refunded, so this is money currently held, not merely ever-captured.</summary>
    public decimal PaidRevenue { get; set; }

    /// <summary>Sum of Payment.Amount for attempts currently Refunded — always reported separately, never netted against PaidRevenue.</summary>
    public decimal RefundedAmount { get; set; }

    public string Currency { get; set; } = "CHF";
}
