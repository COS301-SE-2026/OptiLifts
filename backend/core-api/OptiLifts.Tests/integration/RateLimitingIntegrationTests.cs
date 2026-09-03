using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OptiLifts.Tests.Integration.IntegrationDb;
using Xunit;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class RateLimitingIntegrationTests : IntegrationTestBase
{
    public RateLimitingIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private sealed record RateLimitErrorResponse(string? Type, string? Title, int Status, string? Detail, int RetryAfterSeconds);

    [Fact]
    public async Task AuthEndpoint_WhenLimitExceeded_Returns429TooManyRequests()
    {
        // Custom factory with strict rate limits for testing
        using var customFactory = Fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Enabled", "true");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "2");
            builder.UseSetting("RateLimiting:AuthWindowSeconds", "60");
            builder.UseSetting("RateLimiting:QueueLimit", "0");
        });

        using var client = customFactory.CreateClient();
        var clientIp = "192.0.2.1";

        using var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { Email = "user@example.com", Password = "WrongPassword" })
        };
        req1.Headers.Add("X-Forwarded-For", clientIp);
        var res1 = await client.SendAsync(req1);
        res1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { Email = "user@example.com", Password = "WrongPassword" })
        };
        req2.Headers.Add("X-Forwarded-For", clientIp);
        var res2 = await client.SendAsync(req2);
        res2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Third request exceeds the limit of 2
        using var req3 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { Email = "user@example.com", Password = "WrongPassword" })
        };
        req3.Headers.Add("X-Forwarded-For", clientIp);
        var res3 = await client.SendAsync(req3);
        res3.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        res3.Headers.Contains("Retry-After").Should().BeTrue();

        var body = await res3.Content.ReadFromJsonAsync<RateLimitErrorResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be(429);
        body.Title.Should().Be("Too Many Requests");
        body.Detail.Should().Contain("Rate limit exceeded");
    }

    [Fact]
    public async Task DifferentClientIps_HaveIndependentRateLimitQuotas()
    {
        using var customFactory = Fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Enabled", "true");
            builder.UseSetting("RateLimiting:AuthPermitLimit", "1");
            builder.UseSetting("RateLimiting:AuthWindowSeconds", "60");
            builder.UseSetting("RateLimiting:QueueLimit", "0");
        });

        using var client = customFactory.CreateClient();
        var clientIpA = "192.0.2.10";
        var clientIpB = "192.0.2.20";

        // Client A makes request 1 -> 401 (consumed permit 1)
        using var reqA1 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { Email = "a@example.com", Password = "Wrong" })
        };
        reqA1.Headers.Add("X-Forwarded-For", clientIpA);
        var resA1 = await client.SendAsync(reqA1);
        resA1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Client A makes request 2 -> 429 Too Many Requests
        using var reqA2 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { Email = "a@example.com", Password = "Wrong" })
        };
        reqA2.Headers.Add("X-Forwarded-For", clientIpA);
        var resA2 = await client.SendAsync(reqA2);
        resA2.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Client B makes request 1 -> should succeed independently (401, not 429)
        using var reqB1 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { Email = "b@example.com", Password = "Wrong" })
        };
        reqB1.Headers.Add("X-Forwarded-For", clientIpB);
        var resB1 = await client.SendAsync(reqB1);
        resB1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthCheck_BypassesRateLimiting()
    {
        using var customFactory = Fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Enabled", "true");
            builder.UseSetting("RateLimiting:DefaultPermitLimit", "1");
            builder.UseSetting("RateLimiting:DefaultWindowSeconds", "60");
            builder.UseSetting("RateLimiting:QueueLimit", "0");
        });

        using var client = customFactory.CreateClient();
        var clientIp = "192.0.2.50";

        for (int i = 0; i < 5; i++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, "/api/healthCheck");
            req.Headers.Add("X-Forwarded-For", clientIp);
            var res = await client.SendAsync(req);
            res.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task WhenRateLimitingDisabled_AuthEndpointsPassThroughWithoutLimits()
    {
        using var customFactory = Fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimiting:Enabled", "false");
        });

        using var client = customFactory.CreateClient();
        var clientIp = "192.0.2.99";

        // Making multiple requests does not trigger rate limiting or throw policy missing exceptions
        for (int i = 0; i < 5; i++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new { Email = "user@example.com", Password = "WrongPassword" })
            };
            req.Headers.Add("X-Forwarded-For", clientIp);
            var res = await client.SendAsync(req);
            res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
