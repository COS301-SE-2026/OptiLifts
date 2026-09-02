using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Application.Auth.Register;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;
using Xunit;

namespace OptiLifts.Tests.Unit.Authentication;

public class RegisterUserHandlerTests
{
    private static OptiLiftsDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;

        var ctx = new OptiLiftsDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    //normal user registration
    [Fact]
    public async Task HandleShouldCreateUserAndReturnTokenWhenEmailIsUnique()
    {
        //spins up unit test in memory sqlite db
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var hasherMock = new Mock<IPasswordHasher>();
        hasherMock.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(p => $"HASHED_{p}");

        var jwtMock = new Mock<IJwtTokenService>();
        jwtMock.Setup(j => j.CreateToken(It.IsAny<User>())).Returns("FAKE_TOKEN");

        var handler = new RegisterUserHandler(context, hasherMock.Object, jwtMock.Object);

        var cmd = new RegisterUserCommand("Goat Jordan", "jdawg@gmail.com", "Passw0rd!");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("FAKE_TOKEN");
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.User.Email.Should().Be("jdawg@gmail.com");
        result.User.DisplayName.Should().Be("Goat Jordan");

        var userInDb = await context.Users.FirstOrDefaultAsync(u => u.EmailHash == EmailHasher.HashEmail("jdawg@gmail.com"));
        userInDb.Should().NotBeNull();
        userInDb.PasswordHash.Should().StartWith("HASHED_");

        var repRanges = await context.UserRepRanges
            .Where(r => r.UserId == userInDb.Id)
            .ToListAsync();

        repRanges.Should().HaveCount(2);
        repRanges.Should().ContainSingle(r =>
            r.ExerciseType == UserRepRangeExerciseType.Compound &&
            r.LowerLimit == 6 &&
            r.UpperLimit == 10);
        repRanges.Should().ContainSingle(r =>
            r.ExerciseType == UserRepRangeExerciseType.Isolation &&
            r.LowerLimit == 8 &&
            r.UpperLimit == 12);
    }

    //email alrady exists, should throw exception
    [Fact]
    public async Task HandleShouldThrowDuplicateEmailExceptionWhenEmailExists()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        //add user with the email to the db
        context.Users.Add(new User { Email = "jordan@gmail.com", EmailHash = EmailHasher.HashEmail("jordan@gmail.com"), PasswordHash = "Passw0rd!", DisplayName = "Jordan" });
        await context.SaveChangesAsync();

        var hasherMock = new Mock<IPasswordHasher>();
        var jwtMock = new Mock<IJwtTokenService>();

        var handler = new RegisterUserHandler(context, hasherMock.Object, jwtMock.Object);
        var cmd = new RegisterUserCommand("Jordan", "jordan@gmail.com", "P@ssw0rd!");

        await Assert.ThrowsAsync<DuplicateEmailException>(async () => await handler.Handle(cmd, CancellationToken.None));
    }
}
