using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MultiGuide.Api.Auth;

public static class JwtAuthenticationExtensions
{
    public const string AdminPolicyName = "AdminOnly";

    public static IServiceCollection AddSupabaseJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSecret = configuration["Supabase:JwtSecret"] ?? configuration["Supabase__JwtSecret"];
        var issuer = configuration["Supabase:JwtIssuer"] ?? configuration["Supabase__JwtIssuer"];

        if (string.IsNullOrWhiteSpace(issuer))
        {
            var supabaseUrl = configuration["Supabase:Url"] ?? configuration["Supabase__Url"];
            if (!string.IsNullOrWhiteSpace(supabaseUrl))
            {
                issuer = $"{supabaseUrl.TrimEnd('/')}/auth/v1";
            }
        }

        var audience = configuration["Supabase:JwtAudience"] ?? configuration["Supabase__JwtAudience"] ?? "authenticated";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                ValidIssuer = issuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            if (!string.IsNullOrWhiteSpace(jwtSecret))
            {
                tokenValidationParameters.ValidateIssuerSigningKey = true;
                tokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            }
            else if (!string.IsNullOrWhiteSpace(issuer))
            {
                options.MetadataAddress = $"{issuer.TrimEnd('/')}/.well-known/openid-configuration";
            }

            options.TokenValidationParameters = tokenValidationParameters;
        });

        // Đăng ký Policy và Handler cho vai trò Admin
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new AdminRoleRequirement());
            });
        });

        services.AddSingleton<IAuthorizationHandler, AdminRoleHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationMiddlewareResultHandler>();

        return services;
    }
}
