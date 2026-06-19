using Microsoft.EntityFrameworkCore;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;

namespace OptiLifts.Infrastructure.Database.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedUsersAsync(dbContext, cancellationToken);
        await SeedMusclesAsync(dbContext, cancellationToken);
        await SeedExercisesAsync(dbContext, cancellationToken);
    }

    private static async Task SeedUsersAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken)
    {
        var usersToEnsure = new[]
        {
            new { Email = "test@optilifts.com", Password = "TestPassword123!", DisplayName = "Test Athlete" },
            new { Email = "demo2@optilifts.com", Password = "DemoPass456$", DisplayName = "Demo Two" }
        };

        foreach (var u in usersToEnsure)
        {
            var emailHash = EmailHasher.HashEmail(u.Email);
            if (!await dbContext.Users.AnyAsync(x => x.EmailHash == emailHash, cancellationToken))
            {
                dbContext.Users.Add(new User
                {
                    Email = u.Email,
                    EmailHash = emailHash,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(u.Password),
                    DisplayName = u.DisplayName
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedMusclesAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken)
    {
        var muscleNames = new[]
        {
            "Chest", "Back", "Shoulders", "Biceps", "Triceps",
            "Quadriceps", "Hamstrings", "Glutes", "Calves",
            "Abdominals", "Forearms", "Trapezius"
        };

        foreach (var name in muscleNames)
        {
            if (!await dbContext.Muscles.AnyAsync(m => m.Name == name, cancellationToken))
            {
                dbContext.Muscles.Add(new Muscle { Name = name });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedExercisesAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken)
    {
        // map muscle name -> id so exercises can reference the primary_muscle FK
        var muscles = await dbContext.Muscles
            .ToDictionaryAsync(m => m.Name, m => m.Id, cancellationToken);

        var exercisesToEnsure = new[]
        {
            new { Name = "Barbell Bench Press", Type = ExerciseType.WeightReps,        Muscle = "Chest",       Mechanic = "Compound",  Equipment = "Barbell" },
            new { Name = "Pull Up",             Type = ExerciseType.BodyweightReps,    Muscle = "Back",        Mechanic = "Compound",  Equipment = "Bodyweight" },
            new { Name = "Barbell Back Squat",  Type = ExerciseType.WeightReps,        Muscle = "Quadriceps",  Mechanic = "Compound",  Equipment = "Barbell" },
            new { Name = "Deadlift",            Type = ExerciseType.WeightReps,        Muscle = "Hamstrings",  Mechanic = "Compound",  Equipment = "Barbell" },
            new { Name = "Overhead Press",      Type = ExerciseType.WeightReps,        Muscle = "Shoulders",   Mechanic = "Compound",  Equipment = "Barbell" },
            new { Name = "Dumbbell Bicep Curl", Type = ExerciseType.WeightReps,        Muscle = "Biceps",      Mechanic = "Isolation", Equipment = "Dumbbell" },
            new { Name = "Tricep Pushdown",     Type = ExerciseType.WeightReps,        Muscle = "Triceps",     Mechanic = "Isolation", Equipment = "Cable" },
            new { Name = "Plank",               Type = ExerciseType.Duration,          Muscle = "Abdominals",  Mechanic = "Isolation", Equipment = "Bodyweight" },
            new { Name = "Running",             Type = ExerciseType.DistanceDuration,  Muscle = "Quadriceps",  Mechanic = "Compound",  Equipment = "Bodyweight" }
        };

        foreach (var e in exercisesToEnsure)
        {
            if (!muscles.TryGetValue(e.Muscle, out var primaryMuscleId))
            {
                continue; // muscle missing -> skip rather than violate the FK
            }

            // global/default exercises have no owner (UserId == null)
            if (!await dbContext.Exercises.AnyAsync(x => x.Name == e.Name && x.UserId == null, cancellationToken))
            {
                dbContext.Exercises.Add(new Exercise
                {
                    Name = e.Name,
                    Mechanic = e.Mechanic,
                    Equipment = e.Equipment,
                    ExerciseType = e.Type,
                    PrimaryMuscleId = primaryMuscleId,
                    UserId = null
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
