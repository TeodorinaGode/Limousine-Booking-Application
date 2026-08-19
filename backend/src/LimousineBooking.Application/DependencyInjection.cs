using LimousineBooking.Application.Authentication;
using LimousineBooking.Application.Availability;
using LimousineBooking.Application.Bookings;
using LimousineBooking.Application.Drivers;
using LimousineBooking.Application.Interfaces;
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

        services.AddScoped<IAuthService, LoginHandler>();
        services.AddScoped<IRouteService, RouteService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IDriverService, DriverService>();
        services.AddScoped<IDriverAvailabilityService, DriverAvailabilityService>();
        services.AddScoped<IAvailabilityEvaluationService, AvailabilityEvaluationService>();
        services.AddScoped<IBookingReferenceGenerator, BookingReferenceGenerator>();
        services.AddScoped<IAutomaticAssignmentService, AutomaticAssignmentService>();
        services.AddScoped<IPublicBookingService, PublicBookingService>();

        // Further use cases, validators, and mapping profiles will be
        // registered here as they are introduced in subsequent steps.
        return services;
    }
}
