using LimousineBooking.Application.Authentication;
using LimousineBooking.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace LimousineBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, LoginHandler>();

        // Further use cases, validators, and mapping profiles will be
        // registered here as they are introduced in subsequent steps.
        return services;
    }
}
