namespace LimousineBooking.Application.Payments;

/// <summary>
/// Bound from the "PaymentSettings" configuration section. SecretKey/WebhookSecret
/// are always empty in appsettings.json — they must come from environment
/// variables, User Secrets, or a secret store, never committed to source control
/// (same convention already used for Jwt:SecretKey and EmailSettings:Password).
/// </summary>
public class PaymentSettings
{
    public const string SectionName = "PaymentSettings";

    public string Provider { get; set; } = "Stripe";

    /// <summary>When false, FakePaymentService is used instead of Stripe — for local development and automated tests only (section 51/52). Never true→false silently in production.</summary>
    public bool Enabled { get; set; }

    public string Currency { get; set; } = "CHF";

    public string SuccessUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>How long a Checkout Session stays open before it expires (section 28).</summary>
    public int CheckoutExpirationMinutes { get; set; } = 15;

    /// <summary>Only used by FakePaymentService (Enabled = false) to build an absolute, clickable fake-checkout URL for local dev/manual testing — irrelevant once real Stripe is active.</summary>
    public string FakeCheckoutBaseUrl { get; set; } = "http://localhost:5000";
}
