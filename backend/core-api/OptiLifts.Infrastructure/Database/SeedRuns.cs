using Microsoft.EntityFrameworkCore;

namespace OptiLifts.Infrastructure.Database;

// Records which deploy has already seeded, so replicas started later by the same deploy
// (scale-outs, restarts) leave the shared database alone. The key is the Azure Container
// Apps revision (one per deploy), or the container hostname locally (new per `pnpm prod`).
public static class SeedRuns
{
    private const string CreateTableSql =
        "CREATE TABLE IF NOT EXISTS seed_runs (seed_key varchar(200) PRIMARY KEY, seeded_at timestamptz NOT NULL DEFAULT now())";

    // null when there is no stable key (e.g. `dotnet run` outside Docker), so every start seeds
    public static string? GetSeedKey(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var revision = configuration["CONTAINER_APP_REVISION"];
        if (!string.IsNullOrWhiteSpace(revision))
        {
            return revision;
        }

        var inContainer = string.Equals(configuration["DOTNET_RUNNING_IN_CONTAINER"], "true", StringComparison.OrdinalIgnoreCase);
        return inContainer ? Environment.MachineName : null;
    }

    public static async Task<bool> HasRunAsync(OptiLiftsDbContext dbContext, string seedKey, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlRawAsync(CreateTableSql, cancellationToken);

        return await dbContext.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM seed_runs WHERE seed_key = {seedKey}")
            .SingleAsync(cancellationToken) > 0;
    }

    public static async Task RecordAsync(OptiLiftsDbContext dbContext, string seedKey, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlRawAsync(CreateTableSql, cancellationToken);
        await dbContext.Database.ExecuteSqlAsync($"INSERT INTO seed_runs (seed_key) VALUES ({seedKey}) ON CONFLICT DO NOTHING", cancellationToken);
    }
}
