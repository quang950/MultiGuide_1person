using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MultiGuide.Business.DTOs.Client;
using MultiGuide.Business.Services;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Api.Auth;

/// <summary>
/// Middleware xác thực AccessPass token cho các endpoint du khách được đánh dấu [RequireAccessPass] (US-03).
/// </summary>
public class AccessPassMiddleware
{
    public const string HttpContextItemKey = "PassContext";
    private readonly RequestDelegate _next;

    public AccessPassMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPassService passService)
    {
        var endpoint = context.GetEndpoint();
        var requiresPass = endpoint?.Metadata.GetMetadata<RequireAccessPassAttribute>() != null;

        if (!requiresPass)
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Yêu cầu header Authorization: Bearer <access_token> để truy cập tài nguyên này.",
                ErrorCodes.PassMissing);
            return;
        }

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Token truy cập không được để trống.",
                ErrorCodes.PassMissing);
            return;
        }

        try
        {
            var passContext = await passService.ValidateTokenAsync(token, context.RequestAborted);
            context.Items[HttpContextItemKey] = passContext;
            await _next(context);
        }
        catch (ForbiddenException ex)
        {
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                ex.Message,
                ex.Code);
        }
        catch (UnauthorizedException ex)
        {
            await WriteProblemDetailsAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                ex.Message,
                ex.Code);
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string errorCode)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["code"] = errorCode;

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        if (!string.IsNullOrEmpty(traceId))
        {
            problemDetails.Extensions["traceId"] = traceId;
        }

        await context.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json");
    }
}

public static class AccessPassMiddlewareExtensions
{
    public static IApplicationBuilder UseAccessPass(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AccessPassMiddleware>();
    }

    public static PassContext? GetPassContext(this HttpContext context)
    {
        if (context.Items.TryGetValue(AccessPassMiddleware.HttpContextItemKey, out var item) && item is PassContext passContext)
        {
            return passContext;
        }
        return null;
    }
}
