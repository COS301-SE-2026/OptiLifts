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

Env.TraversePath().Load();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dbHost = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB");
var dbUser = Environment.GetEnvironmentVariable("POSTGRES_USER");
var dbPass = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";

builder.Services.AddDbContext<OptiLiftsDbContext>(options =>
    options.UseNpgsql(connectionString));

//register MediatR handlers from Application assembly
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(
    typeof(IAssemblyMarker).Assembly,
    typeof(OptiLiftsDbContext).Assembly));

//register auth implementations
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? throw new InvalidOperationException("JWT_SECRET environment variable is not set.");
var jwtExpiryMinutes = int.TryParse(Environment.GetEnvironmentVariable("JWT_EXP_MINUTES"), out var expiryMinutes)
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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
    await dbContext.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); //authentication middleware 
app.UseAuthorization(); //authorization middleware
app.MapControllers();

await app.RunAsync();
