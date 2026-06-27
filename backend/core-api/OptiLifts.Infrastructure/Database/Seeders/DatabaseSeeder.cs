using Microsoft.EntityFrameworkCore;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;

namespace OptiLifts.Infrastructure.Database.Seeders;

public static class DatabaseSeeder
{
    // Only encryption-critical data (users) is seeded here, because writing through
    // EF is what applies the [Encrypted] value converters. The Alex profile demo
    // rows now live in Database/SqlScripts/seed-demo-data.sql.
    public static async Task SeedAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedUsersAsync(dbContext, cancellationToken);

        if (!await dbContext.Workouts.AnyAsync(cancellationToken))
        {
            var assembly = typeof(DatabaseSeeder).Assembly;
            using var stream = assembly.GetManifestResourceStream("OptiLifts.Infrastructure.Database.SqlScripts.seed-demo-data.sql");

            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                var script = await reader.ReadToEndAsync(cancellationToken);

                await dbContext.Database.ExecuteSqlRawAsync(script, cancellationToken);
            }
        }
    }

    private static async Task SeedUsersAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken)
    {
        var usersToEnsure = new[]
        {
            new
            {
                Email = "test@optilifts.com",
                Password = "TestPassword123!",
                DisplayName = "Test Athlete",
                Level = 5,
                Weight = "82.5",
                Height = "180",
                Sex = "Male",
                DateOfBirth = "1998-04-23",
                Bio = "Powerlifting enthusiast and OptiLifts demo account.",
                Metric = true,
                LightTheme = false
            },
            new
            {
                Email = "demo2@optilifts.com",
                Password = "DemoPass456$",
                DisplayName = "Demo Two",
                Level = 3,
                Weight = "68.0",
                Height = "170",
                Sex = "Female",
                DateOfBirth = "2000-09-12",
                Bio = "Hypertrophy-focused lifter trying out the app.",
                Metric = true,
                LightTheme = true
            },
            new
            {
                // Rich profile-page demo account. Workouts/logs are seeded in seed-demo-data.sql.
                Email = "gymgoer@gmail.com",
                Password = "GymGoer123!",
                DisplayName = "Alex",
                Level = 12,
                Weight = "78.0",
                Height = "182",
                Sex = "Male",
                DateOfBirth = "1999-02-14",
                Bio = "Loves to gym every day all day. This is their favourite app ever.",
                Metric = true,
                LightTheme = false
            }
        };

        foreach (var u in usersToEnsure)
        {
            var emailHash = EmailHasher.HashEmail(u.Email);
            if (!await dbContext.Users.AnyAsync(x => x.EmailHash == emailHash, cancellationToken))
            {
                // Encrypted fields (Email, DisplayName, Weight, Height, Sex, DateOfBirth)
                // are encrypted automatically by the EF value converter on save.
                dbContext.Users.Add(new User
                {
                    Email = u.Email,
                    EmailHash = emailHash,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(u.Password),
                    DisplayName = u.DisplayName,
                    Level = u.Level,
                    Weight = u.Weight,
                    Height = u.Height,
                    Sex = u.Sex,
                    DateOfBirth = u.DateOfBirth,
                    Bio = u.Bio,
                    Metric = u.Metric,
                    LightTheme = u.LightTheme
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
