using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using OptiLifts.Tests.Integration.IntegrationDb;
using Xunit;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class SwaggerEndpointIntegrationTests : IntegrationTestBase
{
    public SwaggerEndpointIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task SwaggerEndpoints_WhenInNonDevelopmentEnvironment_ReturnNotFound()
    {
        // Testing environment by default in DatabaseFixture
        var swaggerJsonResponse = await Client.GetAsync("/swagger/v1/swagger.json");
        swaggerJsonResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var swaggerUiResponse = await Client.GetAsync("/swagger/index.html");
        swaggerUiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SwaggerEndpoints_WhenInProductionEnvironment_ReturnNotFound()
    {
        using var prodFactory = Fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        });

        using var prodClient = prodFactory.CreateClient();

        var swaggerJsonResponse = await prodClient.GetAsync("/swagger/v1/swagger.json");
        swaggerJsonResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var swaggerUiResponse = await prodClient.GetAsync("/swagger/index.html");
        swaggerUiResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SwaggerEndpoints_WhenInDevelopmentEnvironment_ReturnOk()
    {
        using var devFactory = Fixture.Factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });

        using var devClient = devFactory.CreateClient();

        var swaggerJsonResponse = await devClient.GetAsync("/swagger/v1/swagger.json");
        swaggerJsonResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonContent = await swaggerJsonResponse.Content.ReadAsStringAsync();
        jsonContent.Should().Contain("OptiLifts Core API");

        var swaggerUiResponse = await devClient.GetAsync("/swagger/index.html");
        swaggerUiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
