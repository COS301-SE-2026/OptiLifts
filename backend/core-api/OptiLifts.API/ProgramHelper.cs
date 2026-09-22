using System.Text;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Storage;
using OptiLifts.Application.Vision;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Database.Seeders;
using OptiLifts.Infrastructure.Storage;
using OptiLifts.Infrastructure.Vision;

namespace OptiLifts.API;

public static class SecurityExtensions
{
    public static IServiceCollection AuthProgramHelper(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        var jwtSecret = configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is not set.");
        var jwtExpiryMinutes = int.TryParse(configuration["JWT_EXP_MINUTES"], out var expiryMinutes)
            ? expiryMinutes
            : 1440;

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(jwtSecret);
        }
        catch (FormatException)
        {
            keyBytes = Encoding.UTF8.GetBytes(jwtSecret);
        }

        services.AddSingleton<IJwtTokenService>(_ => new JwtTokenService(jwtSecret, jwtExpiryMinutes));
        var googleClientId = configuration["GOOGLE_CLIENT_ID"] ?? string.Empty;
        services.AddSingleton<IGoogleAuthService>(_ => new GoogleAuthService(googleClientId));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
                };

                // get token from http cookie
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (context.Request.Cookies.TryGetValue("access_token", out var token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["POSTGRES_CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var dbHost = configuration["POSTGRES_HOST"];
            var dbPort = configuration["POSTGRES_PORT"];
            var dbName = configuration["POSTGRES_DB"];
            var dbUser = configuration["POSTGRES_USER"];
            var dbPass = configuration["POSTGRES_PASSWORD"];

            connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";
        }

        services.AddDbContext<OptiLiftsDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }

    public static async Task ApplyDatabaseMigrationsAndSeedingAsync(this WebApplication app, IConfiguration configuration)
    {
        var runMigrations = !string.Equals(configuration["RUN_MIGRATIONS"], "false", StringComparison.OrdinalIgnoreCase);
        if (runMigrations)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
            await dbContext.Database.MigrateAsync();

            var seed = string.Equals(configuration["DEV_SEEDING"], "true", StringComparison.OrdinalIgnoreCase);
            if (seed)
            {
                var isE2e = string.Equals(configuration["E2E_TESTING"], "true", StringComparison.OrdinalIgnoreCase);
                var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
                await DatabaseSeeder.SeedAsync(dbContext, blobStorage, isE2e);
            }
        }
    }
}

public static class OptiVisionExtensions
{
    public static IServiceCollection AddAzureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var azureStorageConnection = configuration.GetConnectionString("AzureStorage");
        if (string.IsNullOrWhiteSpace(azureStorageConnection))
        {
            azureStorageConnection = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__AZURESTORAGE")
                ?? Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        }
        if (string.IsNullOrWhiteSpace(azureStorageConnection))
        {
            azureStorageConnection = "UseDevelopmentStorage=true;";
        }

        services.AddSingleton(new BlobServiceClient(azureStorageConnection));
        services.AddScoped<IBlobStorageService, AzureBlobStorageService>();
        services.AddScoped<IOptiVisionStorageService, OptiVisionStorageService>();

        var serviceBusConnection = configuration.GetConnectionString("ServiceBus");
        if (string.IsNullOrWhiteSpace(serviceBusConnection))
        {
            serviceBusConnection = Environment.GetEnvironmentVariable("CONNECTIONSTRINGS__SERVICEBUS")
                ?? Environment.GetEnvironmentVariable("AZURE_SERVICE_BUS_CONNECTION_STRING")
                ?? Environment.GetEnvironmentVariable("SERVICEBUS_CONNECTION_STRING");
        }
        if (string.IsNullOrWhiteSpace(serviceBusConnection))
        {
            serviceBusConnection = "Endpoint=sb://optilifts.servicebus.windows.net/;SharedAccessKeyName=SendAccess;SharedAccessKey=dummykey=;";
        }

        var serviceBusQueueName = configuration["SERVICE_BUS_QUEUE_NAME"]
            ?? Environment.GetEnvironmentVariable("SERVICE_BUS_QUEUE_NAME")
            ?? "cv-jobs-queue";

        services.AddSingleton(new ServiceBusClient(serviceBusConnection));
        services.AddSingleton(sp => sp.GetRequiredService<ServiceBusClient>().CreateSender(serviceBusQueueName));

        return services;
    }

    public static IServiceCollection AddAiIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        var geminiBaseUrl = configuration["GEMINI_BASE_URL"]
            ?? Environment.GetEnvironmentVariable("GEMINI_BASE_URL")
            ?? "https://generativelanguage.googleapis.com/";
        if (!geminiBaseUrl.EndsWith('/'))
        {
            geminiBaseUrl += "/";
        }

        services.AddSingleton<IVisionPromptBuilder, VisionPromptBuilder>();
        services.AddHttpClient<IGeminiClient, GeminiClient>(client =>
        {
            client.BaseAddress = new Uri(geminiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        var aiApiUrl = configuration["AI_API_URL"] ?? configuration["AiApiBaseUrl"] ?? "http://localhost:8000";
        if (!aiApiUrl.EndsWith('/'))
        {
            aiApiUrl += "/";
        }
        services.AddHttpClient("AiApi", client =>
        {
            client.BaseAddress = new Uri(aiApiUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static async Task InitializeLocalEmulatorStorageAsync(this WebApplication app)
    {
        var storageConnectionString = app.Configuration.GetConnectionString("AzureStorage") ?? app.Configuration["ConnectionStrings:AzureStorage"];
        bool isLocalEmulator = storageConnectionString != null &&
                (storageConnectionString.Contains("UseDevelopmentStorage=true") ||
                 storageConnectionString.Contains("azurite:10000") ||
                 storageConnectionString.Contains("127.0.0.1:10000"));

        if (isLocalEmulator && !app.Environment.IsEnvironment("Testing"))
        {
            try
            {
                var blobServiceClient = app.Services.GetRequiredService<BlobServiceClient>();
                var containers = new[] { "jobs", "exercises", "optivision-payloads", "profile-pictures" };
                foreach (var container in containers)
                {
                    var containerClient = blobServiceClient.GetBlobContainerClient(container);
                    await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
                }
            }
            catch (Exception ex)
            {
                app.Logger.LogWarning(ex, "Failed to initialize Azure Blob Storage containers on startup.");
            }
        }
    }
}