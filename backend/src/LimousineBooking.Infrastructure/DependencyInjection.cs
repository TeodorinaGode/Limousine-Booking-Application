using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Payments;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Authentication;
using LimousineBooking.Infrastructure.Common;
using LimousineBooking.Infrastructure.Email;
using LimousineBooking.Infrastructure.Payments;
using LimousineBooking.Infrastructure.Persistence;
using LimousineBooking.Infrastructure.Repositories;
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

        services.AddHttpContextAccessor();

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
