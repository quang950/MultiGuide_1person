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

        var useInMemory = configuration["USE_IN_MEMORY_DB"] == "true"
                          || Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") == "true";

        if (useInMemory)
        {
            var dbName = configuration["IN_MEMORY_DB_NAME"] ?? "MultiGuide_InMemory";
            services.AddDbContext<MultiGuideDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });
            return services;
        }

        var connectionString = !string.IsNullOrWhiteSpace(rawConn)
            ? MultiGuideDbContextFactory.ConvertPostgresUriToConnectionString(MultiGuideDbContextFactory.CleanConnectionString(rawConn))
            : MultiGuideDbContextFactory.GetConnectionString();

        services.AddDbContext<MultiGuideDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        return services;
    }
}
