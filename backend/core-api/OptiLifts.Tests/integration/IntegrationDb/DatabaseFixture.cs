using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OptiLifts.Infrastructure.Database;
using Respawn;
using Testcontainers.PostgreSql;

namespace OptiLifts.Tests.Integration.IntegrationDb;


public sealed class DatabaseFixture : IAsyncLifetime
{
    public const string JwtSecret = "tessting_key";

    private readonly PostgreSqlContainer _pg;
    //can only assign these once docker container is done setting up so it cannot be done in constructor
    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!; 

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public DatabaseFixture()
    {
        _pg = new PostgreSqlBuilder("postgres:15-alpine")
            .WithDatabase($"optilifts_integration_tests_db")
            .WithUsername("postgres")
            .WithPassword("test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();

        //allows us to make http requests to api by running api in memory and gives it a custom config to use the testing db instead of local db
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("POSTGRES_CONNECTION_STRING", _pg.GetConnectionString() + ";Pooling=false");
            builder.UseSetting("JWT_SECRET", JwtSecret);
            builder.UseSetting("JWT_EXP_MINUTES", "60");
            builder.UseSetting("FRONTEND_ORIGIN", "localhost:5173");
            builder.UseSetting("RUN_MIGRATIONS", "false");
        });

        var dbOptions = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseNpgsql(_pg.GetConnectionString())
            .Options;

        //run migrations
        await using var db = new OptiLiftsDbContext(dbOptions);
        await db.Database.MigrateAsync();

        //setup respawner to reset db between tests
        _dbConnection = new NpgsqlConnection(_pg.GetConnectionString());
        await _dbConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_dbConnection, new RespawnerOptions
        {
            TablesToIgnore = ["__EFMigrationsHistory"]
        });

    }

    public async Task ResetDb()
    {
        await _respawner.ResetAsync(_dbConnection);

        var dbOptions = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseNpgsql(_pg.GetConnectionString())
            .Options;
        await using var db = new OptiLiftsDbContext(dbOptions);
        await OptiLifts.Infrastructure.Database.Seeders.DatabaseSeeder.SeedAsync(db);
    }

    public async Task DisposeAsync()
    {
        if (_dbConnection != null)
        {
            await _dbConnection.DisposeAsync();
        }
        await Factory.DisposeAsync();
        await _pg.DisposeAsync();
    }


}