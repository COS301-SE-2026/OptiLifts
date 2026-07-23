using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly DatabaseFixture Fixture;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(DatabaseFixture fixture)
    {
        Fixture = fixture;

        //each test class gets own httpclient but same factory and db
        Client = Fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    //will run before every fact to reset db
    public virtual async Task InitializeAsync()
    {
        await Fixture.ResetDb();
    }

    public virtual Task DisposeAsync() => Task.CompletedTask;

    //shared seeder functions

    protected async Task<Guid> SeedUserAsync(string email)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = email,
            EmailHash = EmailHasher.HashEmail(email),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            DisplayName = "Test User"
        });

        db.Folders.Add(new Folder { Name = "Default", UserId = userId });
        await db.SaveChangesAsync();
        return userId;
    }

    protected async Task<Guid> SeedWorkoutAsync(Guid userId, string name)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var workout = new Workout
        {
            Name = name,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        db.Workouts.Add(workout);
        await db.SaveChangesAsync();
        return workout.Id;
    }

    protected async Task<Guid> SeedExerciseAsync(string name)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();

        var primaryMuscle = await db.Muscles.FirstOrDefaultAsync() 
        ?? new Muscle
        {
            Id = Guid.NewGuid(),
            Name = "Chest"
        };
        if (db.Entry(primaryMuscle).State == EntityState.Detached)
        {
            db.Muscles.Add(primaryMuscle);
        }
        var exercise = new Exercise
        {
            Name = name,
            ExerciseType = default,
            Mechanic = "compound",
            Equipment = "bodyweight",
            PrimaryMuscleId = primaryMuscle.Id
        };
        db.Exercises.Add(exercise);
        await db.SaveChangesAsync();
        return exercise.Id;
    }

    protected string GenerateToken(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(DatabaseFixture.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}