using Microsoft.EntityFrameworkCore;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Database.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var usersToEnsure = new[]
        {
            new { Email = "test@optilifts.com", Password = "TestPassword123!", DisplayName = "Test Athlete" },
            new { Email = "demo2@optilifts.com", Password = "DemoPass456$", DisplayName = "Demo Two" }
        };

        foreach (var u in usersToEnsure)
        {
            if (!await dbContext.Users.AnyAsync(x => x.Email == u.Email, cancellationToken))
            {
                dbContext.Users.Add(new User
                {
                    Email = u.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(u.Password),
                    DisplayName = u.DisplayName
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}