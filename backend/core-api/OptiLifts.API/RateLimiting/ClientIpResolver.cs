using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace OptiLifts.API.RateLimiting;

public static class ClientIpResolver
{
    public static string GetClientIpAddress(HttpContext context)
    {
        // 1. Check X-Forwarded-For header (standard for reverse proxies)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            var ips = forwardedFor.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (ips.Length > 0 && !string.IsNullOrWhiteSpace(ips[0]))
            {
                return ips[0];
            }
        }

        // 2. Check X-Real-IP header
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp) && !string.IsNullOrWhiteSpace(realIp))
        {
            return realIp.ToString().Trim();
        }

        // 3. Fallback to connection remote IP address
        if (context.Connection.RemoteIpAddress != null)
        {
            if (context.Connection.RemoteIpAddress.IsIPv4MappedToIPv6)
            {
                return context.Connection.RemoteIpAddress.MapToIPv4().ToString();
            }
            return context.Connection.RemoteIpAddress.ToString();
        }

        return "unknown-client";
    }

    public static string GetPartitionKey(HttpContext context)
    {
        // For authenticated users, rate limit by User ID
        var userId = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        // For anonymous users, rate limit by IP address
        return $"ip:{GetClientIpAddress(context)}";
    }
}
