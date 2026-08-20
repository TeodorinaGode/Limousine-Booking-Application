using LimousineBooking.Application.Interfaces;
using LimousineBooking.Domain.Entities;
using LimousineBooking.Infrastructure.Authentication;
using LimousineBooking.Infrastructure.Common;
using LimousineBooking.Infrastructure.Email;
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

        // Real SMTP delivery only when explicitly enabled — otherwise the
        // dev-mode logger stands in, so the whole notification pipeline is
        // exercisable without a real email account (see EmailSettings.Enabled).
        var emailSettings = configuration.GetSection(EmailSettings.SectionName).Get<EmailSettings>() ?? new EmailSettings();
        if (emailSettings.Enabled)
            services.AddScoped<IEmailService, SmtpEmailService>();
        else
            services.AddScoped<IEmailService, LoggingEmailService>();

        return services;
    }
}
