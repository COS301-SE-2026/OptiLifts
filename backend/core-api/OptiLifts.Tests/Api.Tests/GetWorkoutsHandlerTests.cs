using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Workouts;
using OptiLifts.Application.Workouts.GetWorkouts;

namespace OptiLifts.Tests.Api.Tests;

//test 1: no workouts = empty result
public class GetWorkoutsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsEmpty_WhenNoWorkouts()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;

        using var db = new OptiLiftsDbContext(options);
        db.Database.EnsureCreated();

        var handler = new GetWorkoutsHandler(db);
        var result = await handler.Handle(new GetWorkoutsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
