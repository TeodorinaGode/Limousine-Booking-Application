using LimousineBooking.Application.Account;
using LimousineBooking.Application.Authentication;
using LimousineBooking.Application.Availability;
using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Company;
using LimousineBooking.Application.Contact;
using LimousineBooking.Application.Crm;
using LimousineBooking.Application.Drivers;
using LimousineBooking.Application.Email;
using LimousineBooking.Application.Interfaces;
using LimousineBooking.Application.Map;
using LimousineBooking.Application.Notifications;
using LimousineBooking.Application.Payments;
using LimousineBooking.Application.Reports;
using LimousineBooking.Application.Routes;
using LimousineBooking.Application.Vehicles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LimousineBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BookingSettings>(configuration.GetSection(BookingSettings.SectionName));
        services.Configure<NotificationSettings>(configuration.GetSection(NotificationSettings.SectionName));
        services.Configure<PaymentSettings>(configuration.GetSection(PaymentSettings.SectionName));
        services.Configure<CompanySettings>(configuration.GetSection(CompanySettings.SectionName));
        services.Configure<MapSettings>(configuration.GetSection(MapSettings.SectionName));
        services.Configure<GmailOAuthSettings>(configuration.GetSection(GmailOAuthSettings.SectionName));
        services.Configure<EmailSyncSettings>(configuration.GetSection(EmailSyncSettings.SectionName));

        services.AddScoped<IAuthService, LoginHandler>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IDriverService, DriverService>();
        services.AddScoped<IDriverAvailabilityService, DriverAvailabilityService>();
        services.AddScoped<IAvailabilityEvaluationService, AvailabilityEvaluationService>();
        services.AddScoped<IBookingReferenceGenerator, BookingReferenceGenerator>();
        services.AddScoped<IAutomaticAssignmentService, AutomaticAssignmentService>();
        services.AddScoped<IPublicBookingService, PublicBookingService>();
        services.AddScoped<IAdminBookingService, AdminBookingService>();
        services.AddScoped<IDriverBookingService, DriverBookingService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IPublicPaymentService, PublicPaymentService>();
        services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAdminNotificationService, AdminNotificationService>();
        services.AddScoped<INotificationOutboxProcessor, NotificationOutboxProcessor>();
        services.AddScoped<IPublicVehicleService, PublicVehicleService>();
        services.AddScoped<IPublicCompanyService, PublicCompanyService>();
        services.AddScoped<IPublicContactService, PublicContactService>();
        services.AddScoped<IContactMessageOutboxProcessor, ContactMessageOutboxProcessor>();
        services.AddScoped<IPublicLocationService, PublicLocationService>();
        services.AddScoped<ICrmCompanyService, CrmCompanyService>();
        services.AddScoped<ICrmContactService, CrmContactService>();
        services.AddScoped<ICrmDealService, CrmDealService>();
        services.AddScoped<ICrmActivityService, CrmActivityService>();
        services.AddScoped<ICrmTaskService, CrmTaskService>();
        services.AddScoped<IAdministratorDirectoryService, AdministratorDirectoryService>();
        services.AddScoped<ICrmAnalyticsService, CrmAnalyticsService>();
        services.AddScoped<IEmailConnectionService, EmailConnectionService>();
        services.AddScoped<IEmailAccessTokenProvider, EmailAccessTokenProvider>();
        services.AddScoped<IEmailSyncProcessor, EmailSyncProcessor>();
        services.AddScoped<ICrmInboxService, CrmInboxService>();
        services.AddScoped<ICrmEmailSendService, CrmEmailSendService>();
        services.AddScoped<ICrmEmailTemplateService, CrmEmailTemplateService>();

        // Further use cases, validators, and mapping profiles will be
        // registered here as they are introduced in subsequent steps.
        return services;
    }
}
