using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Auth.Logout;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Authentication;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Tests.Unit.Authentication;

public class LogoutHandlerTests
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
    public async Task Handle_UserLogout_ShouldClearRefreshToken()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var cont = CreateContext(connection);

        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "jordan@gmail.com",
            EmailHash = "emailHash",
            PasswordHash = "passwordHash",
            DisplayName = "Jordan",
            RefreshTokenHash = "refresshHash",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };
        cont.Users.Add(user);
        await cont.SaveChangesAsync();

        var logoutHandler = new LogoutHandler(cont);
        var logoutCommand = new LogoutCommand(userId);

        await logoutHandler.Handle(logoutCommand, CancellationToken.None);

        var userInDb = await cont.Users.SingleAsync(u => u.Id == userId);
        userInDb.RefreshTokenHash.Should().BeNull();
        userInDb.RefreshTokenExpiryTime.Should().BeNull();
    }
}