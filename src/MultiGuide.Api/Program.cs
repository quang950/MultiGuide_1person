using MultiGuide.Business;
using MultiGuide.Data;
using MultiGuide.Data.Context;
using MultiGuide.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container (DI configuration - T-1.8)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddBusinessServices();
builder.Services.AddDataServices(builder.Configuration);

var app = builder.Build();

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

// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MultiGuide API v1");
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
