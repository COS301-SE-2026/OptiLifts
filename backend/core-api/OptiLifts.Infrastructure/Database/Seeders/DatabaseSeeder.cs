using Microsoft.EntityFrameworkCore;
using OptiLifts.Domain.Gamification;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Security;

namespace OptiLifts.Infrastructure.Database.Seeders;

public static class DatabaseSeeder
{
    // Only encryption-critical data (users) is seeded here, because writing through
    // EF is what applies the [Encrypted] value converters. The Alex profile demo
    // rows are also seeded here so the profile page is populated in a fresh local DB.
    public static async Task SeedAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken = default)
    {
        await SeedUsersAsync(dbContext, cancellationToken);
        await SeedAlexProfileAsync(dbContext, cancellationToken);
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

    private static async Task SeedAlexProfileAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken)
    {
        const string alexEmail = "gymgoer@gmail.com";

        var alex = await dbContext.Users
            .FirstOrDefaultAsync(user => user.EmailHash == EmailHasher.HashEmail(alexEmail), cancellationToken);

        if (alex is null)
        {
            return;
        }

        var hasDemoData = await dbContext.Workouts.AnyAsync(
            workout => workout.CreatedBy == alex.Id && (workout.Name == "Pull" || workout.Name == "Push"),
            cancellationToken);

        if (hasDemoData)
        {
            return;
        }

        var muscles = new[]
        {
            await EnsureMuscleAsync(dbContext, "Chest", cancellationToken),
            await EnsureMuscleAsync(dbContext, "Middle Back", cancellationToken),
            await EnsureMuscleAsync(dbContext, "Lats", cancellationToken),
            await EnsureMuscleAsync(dbContext, "Biceps", cancellationToken),
            await EnsureMuscleAsync(dbContext, "Shoulders", cancellationToken),
            await EnsureMuscleAsync(dbContext, "Triceps", cancellationToken),
        };

        var chest = muscles[0];
        var middleBack = muscles[1];
        var lats = muscles[2];
        var biceps = muscles[3];
        var shoulders = muscles[4];
        var triceps = muscles[5];

        var pullWorkout = new Workout
        {
            Name = "Pull",
            CreatedBy = alex.Id,
            CreatedAt = DateTime.UtcNow,
        };

        var pushWorkout = new Workout
        {
            Name = "Push",
            CreatedBy = alex.Id,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.Workouts.AddRange(pullWorkout, pushWorkout);
        await dbContext.SaveChangesAsync(cancellationToken);

        var exercises = new[]
        {
            await EnsureExerciseAsync(dbContext, "Lat Pulldown", "isolated", "machine", ExerciseType.WeightReps, lats.Id, cancellationToken),
            await EnsureExerciseAsync(dbContext, "Seated Cable Row", "compound", "cable", ExerciseType.WeightReps, middleBack.Id, cancellationToken),
            await EnsureExerciseAsync(dbContext, "Pull Up", "compound", "bodyweight", ExerciseType.BodyweightReps, lats.Id, cancellationToken),
            await EnsureExerciseAsync(dbContext, "Dumbbell Bicep Curl", "isolated", "dumbbell", ExerciseType.WeightReps, biceps.Id, cancellationToken),
            await EnsureExerciseAsync(dbContext, "Barbell Bench Press", "compound", "barbell", ExerciseType.WeightReps, chest.Id, cancellationToken),
            await EnsureExerciseAsync(dbContext, "Overhead Press", "compound", "barbell", ExerciseType.WeightReps, shoulders.Id, cancellationToken),
            await EnsureExerciseAsync(dbContext, "Incline Dumbbell Press", "compound", "dumbbell", ExerciseType.WeightReps, chest.Id, cancellationToken),
            await EnsureExerciseAsync(dbContext, "Tricep Pushdown", "isolated", "cable", ExerciseType.WeightReps, triceps.Id, cancellationToken),
        };

        var pullEntries = new[]
        {
            new { Workout = pullWorkout, Exercise = exercises[0], OrderIndex = 1, Sets = 5, Reps = 12, Weight = 45f },
            new { Workout = pullWorkout, Exercise = exercises[1], OrderIndex = 2, Sets = 5, Reps = 10, Weight = 50f },
            new { Workout = pullWorkout, Exercise = exercises[2], OrderIndex = 3, Sets = 4, Reps = 8, Weight = 0f },
            new { Workout = pullWorkout, Exercise = exercises[3], OrderIndex = 4, Sets = 4, Reps = 12, Weight = 14f },
        };

        var pushEntries = new[]
        {
            new { Workout = pushWorkout, Exercise = exercises[4], OrderIndex = 1, Sets = 4, Reps = 8, Weight = 60f },
            new { Workout = pushWorkout, Exercise = exercises[5], OrderIndex = 2, Sets = 4, Reps = 8, Weight = 40f },
            new { Workout = pushWorkout, Exercise = exercises[6], OrderIndex = 3, Sets = 4, Reps = 10, Weight = 30f },
            new { Workout = pushWorkout, Exercise = exercises[7], OrderIndex = 4, Sets = 4, Reps = 12, Weight = 25f },
        };

        await SeedWorkoutAsync(dbContext, pullEntries, alex.Id, cancellationToken);
        await SeedWorkoutAsync(dbContext, pushEntries, alex.Id, cancellationToken);

        var firstWorkoutBadge = await EnsureBadgeAsync(
            dbContext,
            "workout_count",
            "First Workout",
            "Complete your first workout",
            BadgeCategory.Milestone,
            1,
            cancellationToken);
        var tenWorkoutBadge = await EnsureBadgeAsync(
            dbContext,
            "workout_count",
            "10 Workouts",
            "Complete 10 workouts",
            BadgeCategory.Milestone,
            10,
            cancellationToken);
        var fiftyWorkoutBadge = await EnsureBadgeAsync(
            dbContext,
            "workout_count",
            "50 Workouts",
            "Complete 50 workouts",
            BadgeCategory.Milestone,
            50,
            cancellationToken);
        var consistentBadge = await EnsureBadgeAsync(
            dbContext,
            "streak_weeks",
            "Consistent",
            "Train 5 weeks in a row",
            BadgeCategory.Streak,
            5,
            cancellationToken);

        await EnsureUserBadgeAsync(dbContext, alex.Id, firstWorkoutBadge.Id, cancellationToken);
        await EnsureUserBadgeAsync(dbContext, alex.Id, tenWorkoutBadge.Id, cancellationToken);
        await EnsureUserBadgeAsync(dbContext, alex.Id, fiftyWorkoutBadge.Id, cancellationToken);
        await EnsureUserBadgeAsync(dbContext, alex.Id, consistentBadge.Id, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Muscle> EnsureMuscleAsync(OptiLiftsDbContext dbContext, string name, CancellationToken cancellationToken)
    {
        var muscle = await dbContext.Muscles.FirstOrDefaultAsync(current => current.Name == name, cancellationToken);
        if (muscle is not null)
        {
            return muscle;
        }

        muscle = new Muscle { Name = name };
        dbContext.Muscles.Add(muscle);
        await dbContext.SaveChangesAsync(cancellationToken);
        return muscle;
    }

    private static async Task<Exercise> EnsureExerciseAsync(
        OptiLiftsDbContext dbContext,
        string name,
        string mechanic,
        string equipment,
        ExerciseType exerciseType,
        Guid primaryMuscleId,
        CancellationToken cancellationToken)
    {
        var exercise = await dbContext.Exercises.FirstOrDefaultAsync(current => current.Name == name && current.UserId == null, cancellationToken);
        if (exercise is not null)
        {
            return exercise;
        }

        exercise = new Exercise
        {
            Name = name,
            Mechanic = mechanic,
            Equipment = equipment,
            ExerciseType = exerciseType,
            PrimaryMuscleId = primaryMuscleId,
            UserId = null,
        };

        dbContext.Exercises.Add(exercise);
        await dbContext.SaveChangesAsync(cancellationToken);
        return exercise;
    }

    private static async Task SeedWorkoutAsync(
        OptiLiftsDbContext dbContext,
        IEnumerable<dynamic> entries,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var workoutEntries = entries.ToList();
        var workout = workoutEntries[0].Workout as Workout;

        if (workout is null)
        {
            return;
        }

        var workoutExercises = new List<WorkoutExercise>();
        var scheduledDay = new DateTime(2026, 3, 23, 17, 0, 0, DateTimeKind.Utc);

        foreach (var entry in workoutEntries)
        {
            var workoutExercise = new WorkoutExercise
            {
                WorkoutId = workout.Id,
                ExerciseId = ((Exercise)entry.Exercise).Id,
                OrderIndex = entry.OrderIndex,
            };

            workoutExercises.Add(workoutExercise);
        }

        dbContext.WorkoutExercises.AddRange(workoutExercises);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var entry in workoutEntries)
        {
            var workoutExercise = workoutExercises.First(current => current.ExerciseId == ((Exercise)entry.Exercise).Id);
            for (var setIndex = 1; setIndex <= entry.Sets; setIndex++)
            {
                dbContext.Sets.Add(new WorkoutSet
                {
                    WorkoutExerciseId = workoutExercise.Id,
                    Type = SetType.Normal,
                    Reps = entry.Reps,
                    Weight = entry.Weight,
                    OrderIndex = setIndex,
                    RestTime = 90,
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        for (var sessionIndex = 0; sessionIndex < 51; sessionIndex++)
        {
            var scheduledAt = scheduledDay + TimeSpan.FromHours(sessionIndex * 41);
            var chosenWorkout = sessionIndex % 2 == 0 ? workoutEntries.First().Workout as Workout : workoutEntries.Last().Workout as Workout;

            if (chosenWorkout is null)
            {
                continue;
            }

            var entry = new ScheduledEntry
            {
                UserId = userId,
                WorkoutId = chosenWorkout.Id,
                Scheduled = scheduledAt,
                Status = ScheduleStatus.Completed,
            };

            dbContext.ScheduledEntries.Add(entry);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.WorkoutLogs.Add(new WorkoutLog
            {
                EntryId = entry.Id,
                StartedAt = scheduledAt,
                CompletedAt = scheduledAt.AddMinutes(65),
                AiModified = false,
                Notes = null,
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<Badge> EnsureBadgeAsync(
        OptiLiftsDbContext dbContext,
        string code,
        string name,
        string description,
        BadgeCategory category,
        int threshold,
        CancellationToken cancellationToken)
    {
        var badge = await dbContext.Badges.FirstOrDefaultAsync(current => current.Name == name, cancellationToken);
        if (badge is not null)
        {
            return badge;
        }

        badge = new Badge
        {
            Code = code,
            Name = name,
            Description = description,
            Category = category,
            Threshold = threshold,
            CreatedAt = DateTime.UtcNow,
        };

        dbContext.Badges.Add(badge);
        await dbContext.SaveChangesAsync(cancellationToken);
        return badge;
    }

    private static async Task EnsureUserBadgeAsync(
        OptiLiftsDbContext dbContext,
        Guid userId,
        Guid badgeId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.UserBadges.AnyAsync(
            current => current.UserId == userId && current.BadgeId == badgeId,
            cancellationToken);

        if (!exists)
        {
            dbContext.UserBadges.Add(new UserBadge
            {
                UserId = userId,
                BadgeId = badgeId,
                EarnedAt = DateTime.UtcNow,
            });
        }
    }
}
