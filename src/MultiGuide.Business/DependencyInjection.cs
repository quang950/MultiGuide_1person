using Microsoft.Extensions.DependencyInjection;

namespace MultiGuide.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Business services will be registered here as they are developed
        return services;
    }
}
