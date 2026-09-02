using FluentAssertions;
using Microsoft.Extensions.Configuration;
using OptiLifts.API.RateLimiting;
using Xunit;

namespace OptiLifts.Tests.Unit.RateLimiting;

public sealed class RateLimitingOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldHaveSensibleDefaults()
    {
        // Act
        var options = new RateLimitingOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.DefaultPermitLimit.Should().Be(100);
        options.DefaultWindowSeconds.Should().Be(60);
        options.AuthPermitLimit.Should().Be(15);
        options.AuthWindowSeconds.Should().Be(60);
        options.AiPermitLimit.Should().Be(20);
        options.AiWindowSeconds.Should().Be(60);
        options.QueueLimit.Should().Be(0);
    }

    [Fact]
    public void ConfigurationBinding_ShouldPopulateCustomValues()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["RateLimiting:Enabled"] = "false",
            ["RateLimiting:DefaultPermitLimit"] = "250",
            ["RateLimiting:DefaultWindowSeconds"] = "120",
            ["RateLimiting:AuthPermitLimit"] = "5",
            ["RateLimiting:AuthWindowSeconds"] = "30",
            ["RateLimiting:AiPermitLimit"] = "10",
            ["RateLimiting:AiWindowSeconds"] = "45",
            ["RateLimiting:QueueLimit"] = "2"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var options = new RateLimitingOptions();

        // Act
        configuration.GetSection(RateLimitingOptions.SectionName).Bind(options);

        // Assert
        options.Enabled.Should().BeFalse();
        options.DefaultPermitLimit.Should().Be(250);
        options.DefaultWindowSeconds.Should().Be(120);
        options.AuthPermitLimit.Should().Be(5);
        options.AuthWindowSeconds.Should().Be(30);
        options.AiPermitLimit.Should().Be(10);
        options.AiWindowSeconds.Should().Be(45);
        options.QueueLimit.Should().Be(2);
    }
}
