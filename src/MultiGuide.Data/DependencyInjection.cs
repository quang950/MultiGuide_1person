using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MultiGuide.Business.Repositories;
using MultiGuide.Business.Storage;
using MultiGuide.Data.Context;
using MultiGuide.Data.Repositories;
using MultiGuide.Data.Storage;

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
        }
        else
        {
            var connectionString = !string.IsNullOrWhiteSpace(rawConn)
                ? MultiGuideDbContextFactory.ConvertPostgresUriToConnectionString(MultiGuideDbContextFactory.CleanConnectionString(rawConn))
                : MultiGuideDbContextFactory.GetConnectionString();

            services.AddDbContext<MultiGuideDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
            });
        }

        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<IPoiRepository, PoiRepository>();
        services.AddScoped<ITranslationRepository, TranslationRepository>();
        services.AddScoped<ILanguageRepository, LanguageRepository>();

        services.Configure<SupabaseStorageOptions>(options =>
        {
            options.Url = configuration["Supabase:Url"]
                ?? configuration["Supabase__Url"]
                ?? Environment.GetEnvironmentVariable("Supabase__Url")
                ?? string.Empty;

            options.ServiceRoleKey = configuration["Supabase:ServiceRoleKey"]
                ?? configuration["Supabase__ServiceRoleKey"]
                ?? Environment.GetEnvironmentVariable("Supabase__ServiceRoleKey")
                ?? string.Empty;

            options.AudioBucket = configuration["Supabase:AudioBucket"]
                ?? configuration["Supabase__AudioBucket"]
                ?? Environment.GetEnvironmentVariable("Supabase__AudioBucket")
                ?? "audio";
        });

        services.AddHttpClient<IFileStorage, SupabaseFileStorage>();

        return services;
    }
}
