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