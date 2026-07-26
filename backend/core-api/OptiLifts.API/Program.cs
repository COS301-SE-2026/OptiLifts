using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using OptiLifts.API;
using OptiLifts.Application;
using OptiLifts.Application.Gamification.Abstraction;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Database.Seeders;
using OptiLifts.Infrastructure.Gamification;
using OptiLifts.Infrastructure.Gamification.Rules;


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

var connectionString = builder.Configuration["POSTGRES_CONNECTION_STRING"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    var dbHost = builder.Configuration["POSTGRES_HOST"];
    var dbPort = builder.Configuration["POSTGRES_PORT"];
    var dbName = builder.Configuration["POSTGRES_DB"];
    var dbUser = builder.Configuration["POSTGRES_USER"];
    var dbPass = builder.Configuration["POSTGRES_PASSWORD"];

    connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";
}

builder.Services.AddDbContext<OptiLiftsDbContext>(options =>
    options.UseNpgsql(connectionString));

//register MediatR handlers from Application assembly
//register MediatR handlers from Application and Infrastructure assemblies
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(IAssemblyMarker).Assembly, typeof(OptiLiftsDbContext).Assembly));

builder.Services.AddScoped<OptiLifts.Application.Storage.IBlobStorageService, OptiLifts.Infrastructure.Storage.AzureBlobStorageService>();

//badges
builder.Services.AddScoped<IBadgeRule, WorkoutCountRule>();
builder.Services.AddScoped<IBadgeAwardingService, BadgeAwardingService>();

//register auth implementations
builder.Services.AuthProgramHelper(builder.Configuration);
var app = builder.Build();

var runMigrations = !string.Equals(builder.Configuration["RUN_MIGRATIONS"], "false", StringComparison.OrdinalIgnoreCase);
if (runMigrations)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
    await dbContext.Database.MigrateAsync();

    var seed = string.Equals(builder.Configuration["DEV_SEEDING"], "true", StringComparison.OrdinalIgnoreCase);
    if (seed)
    {
       //if e2e testing don't add images in seeding func 
        var isE2e = string.Equals(builder.Configuration["E2E_TESTING"], "true", StringComparison.OrdinalIgnoreCase);
        var blobStorage = scope.ServiceProvider.GetRequiredService<OptiLifts.Application.Storage.IBlobStorageService>();
        await DatabaseSeeder.SeedAsync(dbContext, blobStorage, isE2e);
    }
}

// Swagger UI available at http://localhost:<port>/swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "OptiLifts Core API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication(); //authentication middleware 
app.UseAuthorization(); //authorization middleware
app.MapControllers();

//basic health check endpoint, doesn't need a controller as just a simple get rq
app.MapGet("/api/healthCheck", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));
await app.RunAsync();

public partial class Program
{
    protected Program() { }
}
