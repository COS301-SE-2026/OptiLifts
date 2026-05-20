using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Database.Seeders;
using OptiLifts.Application;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Infrastructure.Authentication;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    Env.TraversePath().Load();
}

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
              .AllowAnyMethod();
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

//register auth implementations
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
var jwtSecret = builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET is not set.");
var jwtExpiryMinutes = int.TryParse(builder.Configuration["JWT_EXP_MINUTES"], out var expiryMinutes)
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

builder.Services.AddSingleton<IJwtTokenService>(_ => new JwtTokenService(jwtSecret, jwtExpiryMinutes));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
    });

var app = builder.Build();

var runMigrations = !string.Equals(builder.Configuration["RUN_MIGRATIONS"], "false", StringComparison.OrdinalIgnoreCase);
if (runMigrations)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(dbContext);
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

await app.RunAsync();

public partial class Program
{
    protected Program() { }
}
