using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace OptiLifts.API.RateLimiting;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimitingServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = new RateLimitingOptions();
        configuration.GetSection(RateLimitingOptions.SectionName).Bind(options);

        var isE2e = string.Equals(configuration["E2E_TESTING"], "true", StringComparison.OrdinalIgnoreCase);
        var isExplicitlyConfigured = configuration.GetSection(RateLimitingOptions.SectionName).GetSection(nameof(RateLimitingOptions.Enabled)).Exists();
        if (isE2e && !isExplicitlyConfigured)
        {
            options.Enabled = false;
        }

        services.AddSingleton(options);

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                var logger = context.HttpContext.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("OptiLifts.API.RateLimiting");
                var clientIp = ClientIpResolver.GetClientIpAddress(context.HttpContext);
                logger?.LogWarning("Rate limit exceeded for client {ClientIp} on path {Path}", clientIp, context.HttpContext.Request.Path);

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var retryAfterSeconds = options.DefaultWindowSeconds;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterSpan))
                {
                    retryAfterSeconds = Math.Max(1, (int)retryAfterSpan.TotalSeconds);
                }

                context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

                var errorResponse = new
                {
                    type = "https://httpstatuses.com/429",
                    title = "Too Many Requests",
                    status = StatusCodes.Status429TooManyRequests,
                    detail = "Rate limit exceeded. Please wait before trying again.",
                    retryAfterSeconds
                };

                await context.HttpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken: cancellationToken);
            };

            if (!options.Enabled)
            {
                limiterOptions.AddPolicy(RateLimitPolicies.Auth, _ => RateLimitPartition.GetNoLimiter("disabled"));
                limiterOptions.AddPolicy(RateLimitPolicies.Ai, _ => RateLimitPartition.GetNoLimiter("disabled"));
                limiterOptions.AddPolicy(RateLimitPolicies.Default, _ => RateLimitPartition.GetNoLimiter("disabled"));
                return;
            }

            limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var path = httpContext.Request.Path.Value ?? string.Empty;
                if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
                    path.Equals("/api/healthCheck", StringComparison.OrdinalIgnoreCase))
                {
                    return RateLimitPartition.GetNoLimiter("bypass");
                }

                var key = ClientIpResolver.GetPartitionKey(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.DefaultPermitLimit,
                    Window = TimeSpan.FromSeconds(options.DefaultWindowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = options.QueueLimit,
                    AutoReplenishment = true
                });
            });

            limiterOptions.AddPolicy(RateLimitPolicies.Auth, httpContext =>
            {
                var clientIp = ClientIpResolver.GetClientIpAddress(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.AuthPermitLimit,
                    Window = TimeSpan.FromSeconds(options.AuthWindowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = options.QueueLimit,
                    AutoReplenishment = true
                });
            });

            limiterOptions.AddPolicy(RateLimitPolicies.Ai, httpContext =>
            {
                var key = ClientIpResolver.GetPartitionKey(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.AiPermitLimit,
                    Window = TimeSpan.FromSeconds(options.AiWindowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = options.QueueLimit,
                    AutoReplenishment = true
                });
            });

            limiterOptions.AddPolicy(RateLimitPolicies.Default, httpContext =>
            {
                var key = ClientIpResolver.GetPartitionKey(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.DefaultPermitLimit,
                    Window = TimeSpan.FromSeconds(options.DefaultWindowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = options.QueueLimit,
                    AutoReplenishment = true
                });
            });
        });

        return services;
    }
}