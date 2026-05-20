using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Workouts.CreateWorkout;
using OptiLifts.Application.Workouts.GetWorkouts;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Workouts;

namespace OptiLifts.Tests.Integration;

//integration test 1: POST workout with no sets returns 201 and saved result
//integration test 2: POST workout with sets saves sets to db
//integration test 3: POST workout unauthorized returns 401
//integration test 4: POST workout then GET returns it in list (end-to-end)

public class WorkoutsPageIntegrationTests
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

    private static WorkoutsController BuildController(ISender sender, Guid userId)
    {
        var claims = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
        }));

        return new WorkoutsController(sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claims }
            }
        };
    }

    private static async Task<(Guid userId, Guid folderId)> SeedUserAndFolder(OptiLiftsDbContext db, string email)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, Email = email, PasswordHash = "x", DisplayName = "u" });
        var folder = new Folder { Name = "Default", UserId = userId };
        db.Folders.Add(folder);
        await db.SaveChangesAsync();
        folder = await db.Folders.FirstAsync(f => f.UserId == userId);
        return (userId, folder.Id);
    }

    [Fact]
    public async Task CreateWorkout_ReturnsCreated_WhenRequestIsValid()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = BuildDb(conn);

        var (userId, _) = await SeedUserAndFolder(db, "a@example.com");

        var handler = new CreateWorkoutHandler(db);
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<CreateWorkoutCommand>(), It.IsAny<CancellationToken>()))
            .Returns<CreateWorkoutCommand, CancellationToken>((cmd, ct) => handler.Handle(cmd, ct));

        var controller = BuildController(sender.Object, userId);
        var request = new CreateWorkoutRequest(null, "Push Day A", 1, []);

        var result = await controller.CreateWorkout(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var body = created.Value.Should().BeOfType<CreateWorkoutResult>().Subject;
        body.Name.Should().Be("Push Day A");
        body.DayIndex.Should().Be(1);
        body.WorkoutId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateWorkout_ReturnsUnauthorized_WhenSubClaimMissing()
    {
        var sender = new Mock<ISender>();
        var controller = new WorkoutsController(sender.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            }
        };

        var result = await controller.CreateWorkout(
            new CreateWorkoutRequest(null, "Push Day", null, []),
            CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateWorkout_SavesSets_WhenSetsProvided()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = BuildDb(conn);

        var (userId, _) = await SeedUserAndFolder(db, "b@example.com");

        var exercise = new Exercise { Name = "Bench", PrimaryMuscles = ["Chest"], Category = "Strength" };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        exercise = await db.Exercises.FirstAsync(e => e.Name == "Bench");

        var sets = new List<CreateWorkoutSetRequest>
        {
            new(exercise.Id, "Normal", 10, 60f, 0, 90),
            new(exercise.Id, "Normal", 8,  80f, 1, 90),
        };

        var handler = new CreateWorkoutHandler(db);
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<CreateWorkoutCommand>(), It.IsAny<CancellationToken>()))
            .Returns<CreateWorkoutCommand, CancellationToken>((cmd, ct) => handler.Handle(cmd, ct));

        var controller = BuildController(sender.Object, userId);
        var request = new CreateWorkoutRequest(null, "Push Day B", null, sets);

        var result = await controller.CreateWorkout(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var body = created.Value.Should().BeOfType<CreateWorkoutResult>().Subject;

        var savedSets = db.Sets.Where(s => s.WorkoutId == body.WorkoutId).ToList();
        savedSets.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateThenGet_ReturnsWorkout_InList()
    {
        using var conn = new SqliteConnection("DataSource=:memory:");
        conn.Open();
        using var db = BuildDb(conn);

        var (userId, _) = await SeedUserAndFolder(db, "c@example.com");

        var createHandler = new CreateWorkoutHandler(db);
        var getHandler = new GetWorkoutsHandler(db);

        var createSender = new Mock<ISender>();
        createSender
            .Setup(s => s.Send(It.IsAny<CreateWorkoutCommand>(), It.IsAny<CancellationToken>()))
            .Returns<CreateWorkoutCommand, CancellationToken>((cmd, ct) => createHandler.Handle(cmd, ct));

        var getSender = new Mock<ISender>();
        getSender
            .Setup(s => s.Send(It.IsAny<GetWorkoutsQuery>(), It.IsAny<CancellationToken>()))
            .Returns<GetWorkoutsQuery, CancellationToken>((q, ct) => getHandler.Handle(q, ct));

        var createController = BuildController(createSender.Object, userId);
        var getController = BuildController(getSender.Object, userId);

        await createController.CreateWorkout(
            new CreateWorkoutRequest(null, "Leg Day", 3, []),
            CancellationToken.None);

        var getResult = await getController.GetWorkouts(CancellationToken.None);
        var ok = getResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var workouts = ok.Value.Should().BeAssignableTo<IReadOnlyList<WorkoutCardDto>>().Subject;

        workouts.Should().HaveCount(1);
        workouts[0].Name.Should().Be("Leg Day");
    }
}
