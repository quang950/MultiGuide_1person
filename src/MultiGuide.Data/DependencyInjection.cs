using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Data.Context;

namespace MultiGuide.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataServices(this IServiceCollection services, IConfiguration configuration)
    {
        var rawConn = configuration.GetConnectionString("DefaultConnection")
                      ?? configuration["DATABASE_URL"]
                      ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        var connectionString = !string.IsNullOrWhiteSpace(rawConn)
            ? MultiGuideDbContextFactory.ConvertPostgresUriToConnectionString(rawConn)
            : MultiGuideDbContextFactory.GetConnectionString();

        services.AddDbContext<MultiGuideDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
