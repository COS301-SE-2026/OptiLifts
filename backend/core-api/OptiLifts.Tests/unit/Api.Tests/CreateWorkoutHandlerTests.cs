using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Workouts;
using OptiLifts.Application.Workouts.CreateWorkout;

namespace OptiLifts.Tests.Api.Tests;

//test case 1 - workout with no sets is created and persisted
//test case 2 - workout with sets is created and sets are persisted
//test case 3 - result matches the saved workout data

public class CreateWorkoutHandlerTests
{
    private static OptiLiftsDbContext BuildDb(SqliteConnection conn)
    {
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(conn)
            .Options;
        var db = new OptiLiftsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task<(Guid userId, Guid folderId)> SeedUserAndFolder(OptiLiftsDbContext db, string email)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new OptiLifts.Domain.Users.User
        {
            Id = userId,
            Email = email,
            PasswordHash = "x",
            DisplayName = "u"
        });
        var folder = new Folder { Name = "Default", UserId = userId };
        db.Folders.Add(folder);
        await db.SaveChangesAsync();
        folder = await db.Folders.FirstAsync(f => f.UserId == userId);
        return (userId, folder.Id);
    }

    [Fact]
    public async Task Handle_CreatesWorkout_WithNoSets()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = BuildDb(conn);

        var (userId, folderId) = await SeedUserAndFolder(db, "a@example.com");

        var handler = new CreateWorkoutHandler(db);
        var command = new CreateWorkoutCommand(folderId, "Push Day A", 1, userId, []);

        var result = await handler.Handle(command, CancellationToken.None);

        result.WorkoutId.Should().NotBeEmpty();
        result.Name.Should().Be("Push Day A");
        result.FolderId.Should().Be(folderId);
        result.DayIndex.Should().Be(1);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var saved = await db.Workouts.FindAsync(result.WorkoutId);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Push Day A");
        saved.CreatedBy.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_CreatesWorkout_WithSets()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = BuildDb(conn);

        var (userId, folderId) = await SeedUserAndFolder(db, "b@example.com");

        var exercise = new Exercise { Name = "Bench", PrimaryMuscles = ["Chest"], Category = "Strength" };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        exercise = await db.Exercises.FirstAsync(e => e.Name == "Bench");

        var sets = new List<CreateWorkoutSetDto>
        {
            new(exercise.Id, "Normal", 10, 60f, 0, 90),
            new(exercise.Id, "Normal", 8,  70f, 1, 90),
        };

        var handler = new CreateWorkoutHandler(db);
        var command = new CreateWorkoutCommand(folderId, "Push Day B", null, userId, sets);

        var result = await handler.Handle(command, CancellationToken.None);

        result.WorkoutId.Should().NotBeEmpty();
        result.DayIndex.Should().BeNull();

        var savedSets = db.Sets.Where(s => s.WorkoutId == result.WorkoutId).ToList();
        savedSets.Should().HaveCount(2);
        savedSets.Should().AllSatisfy(s => s.ExerciseId.Should().Be(exercise.Id));
    }

    [Fact]
    public async Task Handle_Result_MatchesSavedWorkout()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = BuildDb(conn);

        var (userId, folderId) = await SeedUserAndFolder(db, "c@example.com");

        var handler = new CreateWorkoutHandler(db);
        var command = new CreateWorkoutCommand(folderId, "Leg Day", 3, userId, []);

        var result = await handler.Handle(command, CancellationToken.None);

        var saved = await db.Workouts.FindAsync(result.WorkoutId);
        saved.Should().NotBeNull();
        saved!.Id.Should().Be(result.WorkoutId);
        saved.Name.Should().Be(result.Name);
        saved.FolderId.Should().Be(result.FolderId);
        saved.DayIndex.Should().Be(result.DayIndex);
    }
}
