using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Business.Services;

namespace MultiGuide.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IAdminContentService, AdminContentService>();
        return services;
    }
}
