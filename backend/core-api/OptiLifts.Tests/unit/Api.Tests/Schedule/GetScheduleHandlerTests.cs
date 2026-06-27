using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.GetSchedule;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Scheduling;
namespace OptiLifts.Tests.Api.Tests;

public sealed class GetScheduleHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEmpty_NoScheduleEntries()
    {
        var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var handler = new GetScheduleHandler(db);

        var result = await handler.Handle(
            new GetScheduleQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(-1)), CancellationToken.None);

        result.Should().BeEmpty();
    }
}