using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace MultiGuide.Data.Context;

public class MultiGuideDbContextFactory : IDesignTimeDbContextFactory<MultiGuideDbContext>
{
    public MultiGuideDbContext CreateDbContext(string[] args)
    {
        var connectionString = GetConnectionString();

        var optionsBuilder = new DbContextOptionsBuilder<MultiGuideDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new MultiGuideDbContext(optionsBuilder.Options);
    }

    public static string GetConnectionString()
    {
        var rawConn = Environment.GetEnvironmentVariable("DATABASE_URL")
                      ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                      ?? Environment.GetEnvironmentVariable("DefaultConnection");

        if (string.IsNullOrWhiteSpace(rawConn))
        {
            return "Host=localhost;Port=5432;Database=multiguide_dev;Username=postgres;Password=postgres";
        }

        return ConvertPostgresUriToConnectionString(rawConn);
    }

    public static string ConvertPostgresUriToConnectionString(string rawUrl)
    {
        if (!rawUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !rawUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return rawConnOrValid(rawUrl);
        }

        try
        {
            var uri = new Uri(rawUrl);
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var database = uri.AbsolutePath.TrimStart('/');
            var port = uri.Port > 0 ? uri.Port : 5432;

            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = uri.Host,
                Port = port,
                Database = database,
                Username = username,
                Password = password,
                SslMode = SslMode.Prefer
            };

            return builder.ConnectionString;
        }
        catch
        {
            return rawUrl;
        }
    }

    private static string rawConnOrValid(string conn) => conn;
}
