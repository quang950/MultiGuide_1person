using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using MultiGuide.Api.Middleware;
using MultiGuide.Business;
using MultiGuide.Data;
using MultiGuide.Data.Context;
using MultiGuide.Data.Seed;
using MultiGuide.Domain.Constants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container (DI configuration - T-1.8, T-1.9)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Global Exception Handler & ProblemDetails (T-1.9 / PRD section 7)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Standard 422 ProblemDetails for Model Validation errors
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Unprocessable Entity",
            Detail = "One or more validation errors occurred.",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["code"] = ErrorCodes.ValidationError;
        problem.Extensions["errors"] = errors;

        return new UnprocessableEntityObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" }
        };
    };
});

// Configure Swagger (T-1.9)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MultiGuide API",
        Version = "v1",
        Description = "Backend API cho hệ thống thuyết minh tự động đa ngôn ngữ MultiGuide (ASP.NET Core .NET 10)"
    });
});

builder.Services.AddBusinessServices();
builder.Services.AddDataServices(builder.Configuration);

var app = builder.Build();

// Use Global Exception Handler middleware early in pipeline
app.UseExceptionHandler();

// Run seeder if enabled via configuration (Seed:Enabled or Seed__Enabled)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = services.GetRequiredService<MultiGuideDbContext>();
        await DbSeeder.SeedDemoDataAsync(dbContext, app.Configuration);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MultiGuide API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();

app.UseAuthorization();

// Health check endpoints (US-17 / T-1.8)
// Liveness probe
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Readiness probe - checks database connectivity
app.MapGet("/health/ready", async (MultiGuideDbContext dbContext, CancellationToken ct) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync(ct);
        if (canConnect)
        {
            return Results.Ok(new { status = "ready", database = "healthy" });
        }

        return Results.Json(
            new { status = "unhealthy", database = "unhealthy" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception ex)
    {
        return Results.Json(
            new { status = "unhealthy", database = "unhealthy", error = ex.Message },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapControllers();

app.Run();

public partial class Program { }
