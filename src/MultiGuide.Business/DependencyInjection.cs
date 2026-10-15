using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Business.Common;
using MultiGuide.Business.Services;

namespace MultiGuide.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IAdminContentService, AdminContentService>();
        services.AddScoped<IAreaService, AreaService>();
        services.AddScoped<IPassService, PassService>();
        services.AddScoped<IGeoService, GeoService>();
        services.AddScoped<IPoiService, PoiService>();
        services.AddScoped<IContentService, ContentService>();
        return services;
    }
}
