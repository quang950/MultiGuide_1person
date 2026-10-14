using System.Diagnostics;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MultiGuide.Domain.Constants;

namespace MultiGuide.Api.Middleware;

public static class RateLimitingExtensions
{
    public const string PassCreationPolicy = "PassCreationRateLimit";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "Quá nhiều yêu cầu tạo pass từ IP của bạn. Vui lòng thử lại sau 1 phút.",
                    Instance = context.HttpContext.Request.Path
                };
                problemDetails.Extensions["code"] = ErrorCodes.RateLimited;

                var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
                if (!string.IsNullOrEmpty(traceId))
                {
                    problemDetails.Extensions["traceId"] = traceId;
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    problemDetails,
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: cancellationToken);
            };

            options.AddPolicy(PassCreationPolicy, httpContext =>
            {
                var clientIp = ResolveClientIp(httpContext);

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: clientIp,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    private static string ResolveClientIp(HttpContext httpContext)
    {
        var forwardedHeader = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedHeader))
        {
            var firstIp = forwardedHeader.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(firstIp))
            {
                return firstIp;
            }
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client-ip";
    }
}
