using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Training.RecordAcuteFatigue;
using OptiLifts.Domain.Training;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Training;

namespace OptiLifts.Tests.Unit.Training;

public class RecordAcuteFatigueHandlerTests
{
    [Fact]
    public async Task Handle_RecordsAcuteFatFlaggedEventScopedToMuscleGroup()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        var db = new OptiLiftsDbContext(new DbContextOptionsBuilder<OptiLiftsDbContext>().UseSqlite(conn).Options);
        await db.Database.EnsureCreatedAsync();

        var user = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid()}@x.com", PasswordHash = "x", DisplayName = "U" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var handler = new RecordAcuteFatigueHandler(db);
        await handler.Handle(new RecordAcuteFatigueCommand(user.Id, "Chest"), CancellationToken.None);

        var evt = await db.TrainingEvents.SingleAsync();
        evt.UserId.Should().Be(user.Id);
        evt.Type.Should().Be(TrainingEventType.AcuteFatigueFlagged);
        evt.Scope.Should().Be("Chest");
    }
}
