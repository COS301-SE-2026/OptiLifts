using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Me;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Tests.Unit.Authentication;

public class GetCurrentUserHandlerTests
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

    [Fact]
    public async Task Handle_UserExists_ReturnsUserDto()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "jordan@gmail.com",
            EmailHash = "emailhash",
            PasswordHash = "passwordhash",
            DisplayName = "Jordan",
            CreatedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new GetCurrentUserHandler(context);
        var query = new GetCurrentUserQuery(userId);

        var result = await handler.Handle(query, CancellationToken.None);
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.DisplayName.Should().Be("Jordan");
        result.Email.Should().Be("jordan@gmail.com");
        result.CreatedAt.Should().Be(new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_UserDoesNotExist_ThrowsKeyNotFoundException()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        await using var context = CreateContext(connection);

        var handler = new GetCurrentUserHandler(context);
        var query = new GetCurrentUserQuery(Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }
}