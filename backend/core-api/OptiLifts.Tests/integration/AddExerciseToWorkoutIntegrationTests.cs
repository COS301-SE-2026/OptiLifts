using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using Testcontainers.PostgreSql;

namespace OptiLifts.Tests.Integration;

public sealed class AddExerciseToWorkoutFixture : IAsyncLifetime
{
    public const string JwtSecret = "test_secret_key_for_integration_tests_only";

    private readonly string _dbName = $"optilifts_tests_{Guid.NewGuid():N}";
    private readonly PostgreSqlContainer _postgres;
    private WebApplicationFactory<Program> _factory = null!;

    public AddExerciseToWorkoutFixture()
    {
        _postgres = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase(_dbName)
            .WithUsername("postgres")
            .WithPassword("test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var dbOptions = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var db = new OptiLiftsDbContext(dbOptions))
        {
            await db.Database.MigrateAsync();
            await OptiLifts.Infrastructure.Database.Seeders.DatabaseSeeder.SeedAsync(db);
        }

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("POSTGRES_CONNECTION_STRING", _postgres.GetConnectionString() + ";Pooling=false");
            builder.UseSetting("JWT_SECRET", JwtSecret);
            builder.UseSetting("JWT_EXP_MINUTES", "60");
            builder.UseSetting("FRONTEND_ORIGIN", "localhost:5173");
            builder.UseSetting("RUN_MIGRATIONS", "false");
        });
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        AllowAutoRedirect = false
    });
 
}

public class AddExerciseToWorkoutIntegrationTests : IClassFixture<AddExerciseToWorkoutFixture>
{

}
