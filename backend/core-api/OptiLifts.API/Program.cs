using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using OptiLifts.API;
using OptiLifts.API.RateLimiting;
using OptiLifts.Application;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Gamification.Abstraction;
using OptiLifts.Application.Storage;
using OptiLifts.Application.Vision;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Database.Seeders;
using OptiLifts.Infrastructure.Gamification;
using OptiLifts.Infrastructure.Gamification.Rules;
using OptiLifts.Infrastructure.Storage;
using OptiLifts.Infrastructure.Vision;


if (!string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Testing", StringComparison.OrdinalIgnoreCase))
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        var envFile = Path.Combine(directory.FullName, ".env");
        if (File.Exists(envFile))
        {
            Env.Load(envFile);
            break;
        }

        directory = directory.Parent;
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(options =>
{

    options.Dsn = Environment.GetEnvironmentVariable("CORE_API_SENTRY_DSN") ?? "";
    options.TracesSampleRate = 1.0;
    options.Debug = true;
    var envName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    options.Environment = envName;

    if (envName == "Development" || envName == "Testing")
    {
        options.InitializeSdk = false;
    }
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "OptiLifts Core API",
        Version = "v1",
        Description = "REST API for workout management, exercise tracking, and user data.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "OptiLifts Team",
        },
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

//CORS configuration to allow requests from frontend
var frontendOrigin = builder.Configuration["FRONTEND_ORIGIN"] ?? "localhost:5173";

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(frontendOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddDatabaseInfrastructure(builder.Configuration);

//register MediatR handlers from Application assembly
//register MediatR handlers from Application and Infrastructure assemblies
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(IAssemblyMarker).Assembly, typeof(OptiLiftsDbContext).Assembly));

builder.Services.AddAzureInfrastructure(builder.Configuration);

//platandfat
builder.Services.AddScoped<OptiLifts.Infrastructure.Training.ISeriesBuilder, OptiLifts.Infrastructure.Training.SeriesBuilder>();
builder.Services.AddScoped<OptiLifts.Infrastructure.Training.IPlateauDetectionService, OptiLifts.Infrastructure.Training.PlateauDetectionService>();

//badges
builder.Services.AddScoped<IBadgeRule, WorkoutCountRule>();
builder.Services.AddScoped<IBadgeAwardingService, BadgeAwardingService>();

//register auth implementations
builder.Services.AuthProgramHelper(builder.Configuration);

builder.Services.AddHttpClient<IGoogleCalendarService, GoogleCalendarService>();

builder.Services.AddAiIntegrations(builder.Configuration);
builder.Services.AddRateLimitingServices(builder.Configuration);

var app = builder.Build();

await app.InitializeLocalEmulatorStorageAsync();

await app.ApplyDatabaseMigrationsAndSeedingAsync(builder.Configuration);

if (app.Environment.IsDevelopment())
{
    // Swagger UI available at http://localhost:<port>/swagger
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "OptiLifts Core API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication(); //authentication middleware 
app.UseAuthorization(); //authorization middleware
app.UseRateLimiter(); //rate limiting middleware
app.MapControllers();

//basic health check endpoint, doesn't need a controller as just a simple get rq
app.MapGet("/api/healthCheck", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow })).DisableRateLimiting();
await app.RunAsync();

public partial class Program
{
    protected Program() { }
}
