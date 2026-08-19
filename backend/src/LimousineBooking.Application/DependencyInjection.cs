using Microsoft.Extensions.DependencyInjection;

namespace LimousineBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use cases, validators, and mapping profiles will be registered here
        // as they are introduced in subsequent steps.
        return services;
    }
}
