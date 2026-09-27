using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Infrastructure.Authentication;

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

                // get token from http cookie or SignalR query string
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/hubs") || path.StartsWithSegments("/clash-hub") || path.StartsWithSegments("/api/hubs")))
                        {
                            context.Token = accessToken;
                        }
                        else if (context.Request.Cookies.TryGetValue("access_token", out var token))
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