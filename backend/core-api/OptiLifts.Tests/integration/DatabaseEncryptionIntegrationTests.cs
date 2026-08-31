using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class DatabaseEncryptionIntegrationTests : IntegrationTestBase
{
    public DatabaseEncryptionIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task SensitiveData_ShouldBeEncryptedAtRest_InDatabase()
    {
        var email = "jordan@gmail.com";
        var userId = await SeedUserAsync(email);

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT email FROM users WHERE user_id = @id";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = userId;
        command.Parameters.Add(parameter);

        var encryptedEmail = (string?)await command.ExecuteScalarAsync();

        encryptedEmail.Should().NotBeNull();
        encryptedEmail.Should().NotBe(email);
        encryptedEmail.Should().MatchRegex("^[A-Za-z0-9+/=]+$");//base64
    }
}
