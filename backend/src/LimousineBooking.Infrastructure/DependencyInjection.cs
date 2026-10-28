using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Payments;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Authentication;
using LimousineBooking.Infrastructure.Common;
using LimousineBooking.Infrastructure.Email;
using LimousineBooking.Infrastructure.Payments;
using LimousineBooking.Infrastructure.Persistence;
using LimousineBooking.Infrastructure.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimousineBooking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<PaymentSettings>(configuration.GetSection(PaymentSettings.SectionName));
        services.Configure<GmailOAuthSettings>(configuration.GetSection(GmailOAuthSettings.SectionName));
        services.Configure<EmailSyncSettings>(configuration.GetSection(EmailSyncSettings.SectionName));

        services.AddHttpContextAccessor();
        services.AddHttpClient();

        // Encrypts OAuth refresh tokens at rest (see TokenEncryptionService). Key
        // storage uses Data Protection's default (local filesystem) — fine for a
        // single-instance deployment; a multi-instance production deployment
        // would need a shared key ring (e.g. PersistKeysToDbContext), noted here
        // rather than built pre-emptively since this app has no evidence of
        // running multi-instance today.
        services.AddDataProtection();

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IDriverAvailabilityRepository, DriverAvailabilityRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IAssignmentHistoryRepository, AssignmentHistoryRepository>();
        services.AddScoped<IRideStatusHistoryRepository, RideStatusHistoryRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ITransactionRunner, TransactionRunner>();
        services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentWebhookEventRepository, PaymentWebhookEventRepository>();
        services.AddScoped<IContactMessageRepository, ContactMessageRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<ICrmCompanyRepository, CrmCompanyRepository>();
        services.AddScoped<ICrmContactRepository, CrmContactRepository>();
        services.AddScoped<ICrmDealRepository, CrmDealRepository>();
        services.AddScoped<ICrmActivityRepository, CrmActivityRepository>();
        services.AddScoped<ICrmTaskRepository, CrmTaskRepository>();
        services.AddScoped<ICrmPipelineRepository, CrmPipelineRepository>();
        services.AddScoped<ICrmAnalyticsRepository, CrmAnalyticsRepository>();
        services.AddScoped<IEmailProviderConnectionRepository, EmailProviderConnectionRepository>();
        services.AddScoped<IEmailSyncCheckpointRepository, EmailSyncCheckpointRepository>();
        services.AddScoped<GmailProviderClient>();
        services.AddScoped<IEmailProviderClientFactory, EmailProviderClientFactory>();
        services.AddScoped<IOAuthStateService, OAuthStateService>();
        services.AddScoped<ITokenEncryptionService, TokenEncryptionService>();
        services.AddScoped<IHtmlSanitizerService, HtmlSanitizerService>();
        services.AddScoped<ICrmEmailThreadRepository, CrmEmailThreadRepository>();
        services.AddScoped<ICrmEmailMessageRepository, CrmEmailMessageRepository>();
        services.AddScoped<ICrmEmailTemplateRepository, CrmEmailTemplateRepository>();
        services.AddScoped<IEmailSendOperationRepository, EmailSendOperationRepository>();

        // Real SMTP delivery only when explicitly enabled — otherwise the
        // dev-mode logger stands in, so the whole notification pipeline is
        // exercisable without a real email account (see EmailSettings.Enabled).
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>() ?? new EmailSettings();
        if (emailSettings.Enabled)
            services.AddScoped<IEmailService, SmtpEmailService>();
        else
            services.AddScoped<IEmailService, LoggingEmailService>();

        // FakePaymentService stands in for Stripe unless payments are explicitly
        // enabled (section 51/52) — chosen so local dev/tests never need a real
        // Stripe account, while production must configure real keys or fail fast
        // (never silently bypass payments).
        var paymentSettings = configuration.GetSection(PaymentSettings.SectionName).Get<PaymentSettings>() ?? new PaymentSettings();
        if (paymentSettings.Enabled)
        {
            if (string.IsNullOrWhiteSpace(paymentSettings.SecretKey) || string.IsNullOrWhiteSpace(paymentSettings.WebhookSecret))
            {
                throw new InvalidOperationException(
                    "PaymentSettings.Enabled is true but SecretKey/WebhookSecret are not configured. " +
                    "Set them via environment variables or User Secrets — never in appsettings.json.");
            }

            services.AddScoped<IPaymentService, StripePaymentService>();
        }
        else
        {
            services.AddScoped<IPaymentService, FakePaymentService>();
        }

        return services;
    }
}
