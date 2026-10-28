namespace LimousineBooking.Infrastructure.Persistence.Seed;

/// <summary>
/// The 10 suggested starter templates (Stage 2A §6) — clearly seed content,
/// never anything claiming to have "already been sent." Each uses only the
/// variable tokens CrmEmailTemplateService.RenderAsync actually resolves:
/// contact.firstName/lastName/fullName/email, company.name,
/// deal.name/value/currency, owner.firstName/lastName/email.
/// </summary>
public static class StarterEmailTemplates
{
    public static readonly (string Name, string Category, string Subject, string BodyHtml)[] Templates =
    {
        (
            "Quote Request Response",
            "General",
            "Your ROI Limousinen quote",
            "<p>Dear {{contact.firstName}},</p><p>Thank you for reaching out to ROI Limousinen. Please find your quote details below, and let us know if you'd like any adjustments.</p><p>Best regards,<br/>{{owner.firstName}} {{owner.lastName}}</p>"
        ),
        (
            "Booking Confirmation",
            "Booking",
            "Your booking with ROI Limousinen is confirmed",
            "<p>Dear {{contact.firstName}},</p><p>We're pleased to confirm your upcoming ride. Our chauffeur will be in touch shortly before pickup with any final details.</p><p>Best regards,<br/>{{owner.firstName}} {{owner.lastName}}</p>"
        ),
        (
            "Welcome New Client",
            "Onboarding",
            "Welcome to ROI Limousinen",
            "<p>Dear {{contact.firstName}},</p><p>Welcome aboard! We're delighted to have {{company.name}} as a client and look forward to providing a premium chauffeur experience for your team.</p><p>Best regards,<br/>{{owner.firstName}} {{owner.lastName}}</p>"
        ),
        (
            "Follow-Up After Inquiry",
            "Follow-Up",
            "Following up on your ROI Limousinen inquiry",
            "<p>Dear {{contact.firstName}},</p><p>I wanted to follow up on your recent inquiry — is there anything else you need from us to move forward?</p><p>Best regards,<br/>{{owner.firstName}} {{owner.lastName}}</p>"
        ),
        (
            "Wedding Transfer Proposal",
            "Sales",
            "A proposal for your wedding day transfers",
            "<p>Dear {{contact.firstName}},</p><p>Congratulations! Please find attached our proposal for \"{{deal.name}}\" — {{deal.value}} {{deal.currency}} for a fleet of vehicles dedicated to making your wedding day transfers effortless.</p><p>Best regards,<br/>{{owner.firstName}} {{owner.lastName}}</p>"
        ),
        (
            "Corporate Account Introduction",
            "Sales",
            "Introducing ROI Limousinen's corporate account program",
            "<p>Dear {{contact.firstName}},</p><p>I'd like to introduce {{company.name}} to our corporate account program, designed for reliable, premium transport for your executives and guests.</p><p>Best regards,<br/>{{owner.firstName}} {{owner.lastName}}</p>"
        ),
        (
            "Thank You After Service",
            "Post-Service",
            "Thank you for riding with ROI Limousinen",
            "<p>Dear {{contact.firstName}},</p><p>Thank you for choosing ROI Limousinen. We hope your ride met your expectations, and we'd love to hear any feedback you have.</p><p>Best regards,<br/>{{owner.firstName}} {{owner.lastName}}</p>"
        ),
        (
            "Payment Reminder",
            "Billing",
            "Friendly reminder: outstanding invoice with ROI Limousinen",
            "<p>Dear {{contact.firstName}},</p><p>This is a friendly reminder that an invoice for {{company.name}} remains outstanding. Please let us know if you have any questions.</p><p>Best regards,<br/>{{owner.firstName}} {{owner.lastName}}</p>"
        ),
        (
            "Holiday Season Greeting",
            "Marketing",
            "Season's greetings from ROI Limousinen",
            "<p>Dear {{contact.firstName}},</p><p>Wishing you and the team at {{company.name}} a wonderful holiday season from everyone at ROI Limousinen.</p><p>Best regards,<br/>{{owner.firstName}} {{owner.lastName}}</p>"
        ),
        (
            "Re-engagement — Lapsed Client",
            "Follow-Up",
            "We'd love to welcome you back to ROI Limousinen",
            "<p>Dear {{contact.firstName}},</p><p>It's been a while since we last worked with {{company.name}} — we'd love the opportunity to serve you again. Let us know if we can help with an upcoming trip.</p><p>Best regards,<br/>{{owner.firstName}} {{owner.lastName}}</p>"
        ),
    };
}
