using Microsoft.EntityFrameworkCore;

namespace OptiLifts.Infrastructure.Database;

// Wipes all user data (see SqlScripts/wipe-data.sql) once per token. The token is
// recorded in data_wipes, which the wipe never touches, so later restarts with the
// same WIPE_DATA_TOKEN skip it. Set a new token to wipe again.
public static class DatabaseWiper
{
    public static async Task<bool> WipeOnceAsync(OptiLiftsDbContext dbContext, string token, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "CREATE TABLE IF NOT EXISTS data_wipes (token varchar(200) PRIMARY KEY, wiped_at timestamptz NOT NULL DEFAULT now())",
            cancellationToken);

        var alreadyWiped = await dbContext.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM data_wipes WHERE token = {token}")
            .SingleAsync(cancellationToken) > 0;

        if (alreadyWiped)
        {
            return false;
        }

        var assembly = typeof(DatabaseWiper).Assembly;
        await using var stream = assembly.GetManifestResourceStream("OptiLifts.Infrastructure.Database.SqlScripts.wipe-data.sql")
            ?? throw new InvalidOperationException("wipe-data.sql is not embedded in the assembly");
        using var reader = new StreamReader(stream);
        var script = await reader.ReadToEndAsync(cancellationToken);

        // the wipe and the token are committed together, so a failed wipe is retried on the next start
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(script, cancellationToken);
        await dbContext.Database.ExecuteSqlAsync($"INSERT INTO data_wipes (token) VALUES ({token})", cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }
}
