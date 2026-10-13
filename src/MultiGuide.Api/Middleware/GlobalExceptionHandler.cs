using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MultiGuide.Domain.Constants;
using MultiGuide.Domain.Exceptions;

namespace MultiGuide.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var (statusCode, code, title, detail, errors) = exception switch
        {
            AppException appEx => (
                appEx.StatusCode,
                appEx.Code,
                GetDefaultTitle(appEx.StatusCode),
                appEx.Message,
                appEx.Errors
            ),
            BadHttpRequestException badEx => (
                StatusCodes.Status422UnprocessableEntity,
                ErrorCodes.ValidationError,
                "Unprocessable Entity",
                badEx.Message,
                null
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                ErrorCodes.InternalError,
                "Internal Server Error",
                _env.IsDevelopment() ? exception.Message : "An unexpected internal server error occurred.",
                null
            )
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["code"] = code;

        if (errors != null && errors.Count > 0)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        if (!string.IsNullOrEmpty(traceId))
        {
            problemDetails.Extensions["traceId"] = traceId;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }

    private static string GetDefaultTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status402PaymentRequired => "Payment Required",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
        StatusCodes.Status429TooManyRequests => "Too Many Requests",
        _ => "Internal Server Error"
    };
}
