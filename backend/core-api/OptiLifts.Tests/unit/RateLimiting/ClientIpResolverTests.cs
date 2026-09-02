using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using OptiLifts.API.RateLimiting;
using Xunit;

namespace OptiLifts.Tests.Unit.RateLimiting;

public sealed class ClientIpResolverTests
{
    [Fact]
    public void GetClientIpAddress_WhenXForwardedForIsPresent_ShouldReturnFirstIp()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.195, 70.41.3.18, 150.172.238.178";

        // Act
        var ip = ClientIpResolver.GetClientIpAddress(context);

        // Assert
        ip.Should().Be("203.0.113.195");
    }

    [Fact]
    public void GetClientIpAddress_WhenXRealIpIsPresentAndXForwardedForMissing_ShouldReturnRealIp()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Real-IP"] = "198.51.100.42";

        // Act
        var ip = ClientIpResolver.GetClientIpAddress(context);

        // Assert
        ip.Should().Be("198.51.100.42");
    }

    [Fact]
    public void GetClientIpAddress_WhenHeadersAreMissing_ShouldFallbackToRemoteIpAddress()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.100");

        // Act
        var ip = ClientIpResolver.GetClientIpAddress(context);

        // Assert
        ip.Should().Be("192.168.1.100");
    }

    [Fact]
    public void GetClientIpAddress_WhenRemoteIpIsIPv4MappedToIPv6_ShouldReturnIPv4()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:192.168.1.100");

        // Act
        var ip = ClientIpResolver.GetClientIpAddress(context);

        // Assert
        ip.Should().Be("192.168.1.100");
    }

    [Fact]
    public void GetClientIpAddress_WhenNoIpInfoAvailable_ShouldReturnUnknownClient()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var ip = ClientIpResolver.GetClientIpAddress(context);

        // Assert
        ip.Should().Be("unknown-client");
    }

    [Fact]
    public void GetPartitionKey_WhenUserIsAuthenticated_ShouldReturnUserKey()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var userId = Guid.NewGuid().ToString();
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        var partitionKey = ClientIpResolver.GetPartitionKey(context);

        // Assert
        partitionKey.Should().Be($"user:{userId}");
    }

    [Fact]
    public void GetPartitionKey_WhenUserIsAuthenticatedWithSubClaim_ShouldReturnSubUserKey()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var userId = Guid.NewGuid().ToString();
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId) }, "TestAuth");
        context.User = new ClaimsPrincipal(identity);

        // Act
        var partitionKey = ClientIpResolver.GetPartitionKey(context);

        // Assert
        partitionKey.Should().Be($"user:{userId}");
    }

    [Fact]
    public void GetPartitionKey_WhenUserIsAnonymous_ShouldReturnIpKey()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "10.0.0.1";

        // Act
        var partitionKey = ClientIpResolver.GetPartitionKey(context);

        // Assert
        partitionKey.Should().Be("ip:10.0.0.1");
    }
}
