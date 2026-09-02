using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Storage;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Security;
using OptiLifts.Infrastructure.Training;

namespace OptiLifts.Infrastructure.Database.Seeders;

public static class DatabaseSeeder
{
    // Only encryption-critical data (users) is seeded here, because writing through
    // EF is what applies the [Encrypted] value converters. The Alex profile demo
    // rows now live in Database/SqlScripts/seed-demo-data.sql.
    public static async Task SeedAsync(OptiLiftsDbContext dbContext, IBlobStorageService blobStorage, bool testing = false, CancellationToken cancellationToken = default)
    {
        await SeedUsersAsync(dbContext, cancellationToken);
        await SeedMusclesAsync(dbContext, cancellationToken);
        await SeedExercisesAsync(dbContext, blobStorage, testing, cancellationToken);


        var assembly = typeof(DatabaseSeeder).Assembly;
        using var stream = assembly.GetManifestResourceStream("OptiLifts.Infrastructure.Database.SqlScripts.seed-demo-data.sql");

        if (stream != null)
        {
            using var reader = new StreamReader(stream);
            var script = await reader.ReadToEndAsync(cancellationToken);

            await dbContext.Database.ExecuteSqlRawAsync(script, cancellationToken);
        }


        if (!testing)
        {
            await SeedPlateauDemoDataAsync(dbContext, cancellationToken);
        }
    }

    private static async Task SeedUsersAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken)
    {
        const string testPassword = "TestPassword123!";
        const string testDisplayName = "Test Athlete";
        const string testWeight = "82.5";
        const string testHeight = "180";
        const string testSex = "Male";
        const string testDob = "1998-04-23";
        const string testBio = "Powerlifting enthusiast and OptiLifts demo account.";
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
            }, 
            //NOSONAR - for the e2e tests, is repeat code 
            new
            {
                Email = "test0@optilifts.com",  //NOSONAR 
                Password = testPassword,  //NOSONAR 
                DisplayName = testDisplayName,  //NOSONAR 
                Level = 5,
                Weight = testWeight,
                Height = testHeight,
                Sex = testSex,
                DateOfBirth = testDob,
                Bio = testBio,
                Metric = true,
                LightTheme = false
            },
            new
            {
                Email = "test1@optilifts.com",
                Password = testPassword,
                DisplayName = testDisplayName,
                Level = 5,
                Weight = testWeight,
                Height = testHeight,
                Sex = testSex,
                DateOfBirth = testDob,
                Bio = testBio,
                Metric = true,
                LightTheme = false
            },
            new
            {
                Email = "test2@optilifts.com",
                Password = testPassword,
                DisplayName = testDisplayName,
                Level = 5,
                Weight = testWeight,
                Height = testHeight,
                Sex = testSex,
                DateOfBirth = testDob,
                Bio = testBio,
                Metric = true,
                LightTheme = false
            },
            new
            {
                Email = "test3@optilifts.com",
                Password = testPassword,
                DisplayName = testDisplayName,
                Level = 5,
                Weight = testWeight,
                Height = testHeight,
                Sex = testSex,
                DateOfBirth = testDob,
                Bio = testBio,
                Metric = true,
                LightTheme = false
            },


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

    private static async Task SeedMusclesAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken)
    {
        var muscles = new[]
        {
            "Abductors",
            "Adductors",
            "Abdominals",
            "Obliques",
            "Biceps",
            "Chest",
            "Calves",
            "Forearms",
            "Glutes",
            "Hamstrings",
            "Lats",
            "Lower Back",
            "Middle Back",
            "Upper Back",
            "Quadriceps",
            "Shoulders",
            "Trapezius",
            "Triceps",
            "Front Deltoid",
            "Middle Deltoid",
            "Rear Deltoid"
        };

        foreach (var muscle in muscles)
        {
            if (!await dbContext.Muscles.AnyAsync(m => m.Name == muscle, cancellationToken))
            {
                dbContext.Muscles.Add(new Muscle
                {
                    Name = muscle
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record JsonExercise(
        string Name,
        string Mechanic,
        string Equipment,
        string ExerciseType,
        string PrimaryMuscle,
        List<string> SecondaryMuscles,
        string ImageUrl,
        string Attribution
    );

    private static void AddSecondaryMusclesAsync(OptiLiftsDbContext dbContext, Exercise exercise, List<string> secondaryMuscleNames, Dictionary<string, Guid> muscleIds)
    {
        foreach (var secMuscle in secondaryMuscleNames)
        {
            if (muscleIds.TryGetValue(secMuscle, out var secondaryMuscleId))
            {
                dbContext.SecMuscles.Add(new SecMuscle
                {
                    ExerciseId = exercise.Id,
                    MuscleId = secondaryMuscleId
                });
            }
        }
    }

    private static async Task IntegrationTestsSeeding(OptiLiftsDbContext dbContext, Dictionary<string, Guid> muscleIds, CancellationToken cancellationToken)
    {
        var exercises = new[]
        {
            new { Name = "Barbell Back Squat", Muscle = "Quadriceps", Secondary = new List<string> { "Hamstrings", "Glutes", "Lower Back", "Calves" } },
            new { Name = "Deadlift", Muscle = "Hamstrings", Secondary = new List<string> { "Glutes", "Lower Back", "Middle Back", "Traps", "Quadriceps", "Forearms" } },
            new { Name = "Barbell Bench Press", Muscle = "Chest", Secondary = new List<string> { "Shoulders", "Triceps" } },
            new { Name = "Barbell full squat", Muscle = "Quadriceps", Secondary = new List<string>() },
            new { Name = "Cable lat pulldown", Muscle = "Lats", Secondary = new List<string>() },
            new { Name = "Dumbbell incline bench press", Muscle = "Chest", Secondary = new List<string>() },
            new { Name = "Cable seated row", Muscle = "Middle Back", Secondary = new List<string>() },
            new { Name = "Barbell romanian deadlift", Muscle = "Hamstrings", Secondary = new List<string>() },
            new { Name = "Walking lunge", Muscle = "Quadriceps", Secondary = new List<string>() },
            new { Name = "Barbell seated overhead press", Muscle = "Shoulders", Secondary = new List<string>() },
            new { Name = "Standing calf raise", Muscle = "Calves", Secondary = new List<string>() },
            new { Name = "Pull-up", Muscle = "Lats", Secondary = new List<string>() },
            new { Name = "Dumbbell Alternate Bicep Curl", Muscle = "Biceps", Secondary = new List<string>() },
            new { Name = "Cable triceps pushdown (v-bar)", Muscle = "Triceps", Secondary = new List<string>() },
            new { Name = "Weighted pull-up", Muscle = "Lats", Secondary = new List<string>() },
            new { Name = "Machine assisted pull-up", Muscle = "Lats", Secondary = new List<string>() },
            new { Name = "Weighted front plank", Muscle = "Abdominals", Secondary = new List<string>() },
            new { Name = "Barbell deadlift", Muscle = "Hamstrings", Secondary = new List<string>() }
        };

        foreach (var exercise in exercises)
        {
            var exerciseDb = new Exercise
            {
                Name = exercise.Name,
                ExerciseType = ExerciseType.WeightReps,
                PrimaryMuscleId = muscleIds.GetValueOrDefault(exercise.Muscle, muscleIds.Values.First()),
                Mechanic = "compound",
                Equipment = "barbell"
            };

            dbContext.Exercises.Add(exerciseDb);
            AddSecondaryMusclesAsync(dbContext, exerciseDb, exercise.Secondary, muscleIds);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }


    private static async Task SeedPlateauDemoDataAsync(OptiLiftsDbContext dbContext, CancellationToken cancellationToken)
    {
        const string demoWorkoutName = "Plateau Demo History";

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.EmailHash == EmailHasher.HashEmail("gymgoer@gmail.com"), cancellationToken);
        if (user is null)
        {
            return;
        }

        var alreadySeeded = await dbContext.Workouts.AnyAsync(w => w.CreatedBy == user.Id && w.Name == demoWorkoutName, cancellationToken);
        if (alreadySeeded)
        {
            return;
        }

        var workout = new Workout
        {
            Name = demoWorkoutName,
            CreatedBy = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Workouts.Add(workout);

        // (exercise name, per-session generator over 24 weekly sessions: i=0..11 is the baseline
        // period, i=12..23 is the 12-point detection window). Exercise names are chosen to avoid
        // anything already used by Alex's existing seed-demo-data.sql history, so this synthetic
        // data doesn't mix with his real seeded sessions for the same exercise.
        var scenarios = new List<(string ExerciseName, Func<int, (float Weight, int Reps, float Rpe)> Session)>
        {
            // Progressing: steady rise throughout, effort constant
            ("Barbell front squat", i => (70f + i * 0.9f, 6, 7.5f)),
            ("Barbell close-grip bench press", i => (50f + i * 0.7f, 8, 7.0f)),

            // Plateau: rises for 12 weeks then goes flat for 12 weeks.
            // Deadlift: RPE climbs sharply during the flat period -> recovery-style recommendation.
            ("Barbell deadlift", i => i < 12
                ? (120f + i * 1.5f, 5, 7.5f)
                : (136.5f, 5, 6.0f + (i - 12) * (4.0f / 11f))),
            // Romanian deadlift: RPE stays flat -> exercise-change recommendation.
            ("Dumbbell romanian deadlift", i => i < 12
                ? (40f + i * 0.8f, 10, 7.0f)
                : (48.8f, 10, 7.0f)),

            // Regressing: rises for 12 weeks then genuinely declines for 12 weeks.
            // Bent over row: effort stays flat -> exercise-change recommendation.
            ("Barbell bent over row", i => i < 12
                ? (60f + i, 8, 7.0f)
                : (71f - (i - 11) * 1.5f, 8, 7.0f)),
            // Lateral raise: RPE climbs sharply during the decline -> recovery-style recommendation.
            ("Dumbbell lateral raise", i => i < 12
                ? (10f + i * 0.3f, 12, 7.0f)
                : (13.3f - (i - 11) * 0.4f, 12, 6.0f + (i - 12) * (4.0f / 11f))),
        };

        var affectedExerciseIds = new List<Guid>();
        var startDate = DateTime.UtcNow.AddDays(-24 * 7);
        var workoutExerciseOrderIndex = 0;

        foreach (var (exerciseName, sessionAt) in scenarios)
        {
            var exercise = await dbContext.Exercises.FirstOrDefaultAsync(e => e.Name == exerciseName, cancellationToken);
            if (exercise is null)
            {
                continue;
            }

            affectedExerciseIds.Add(exercise.Id);

            dbContext.WorkoutExercises.Add(new WorkoutExercise
            {
                WorkoutId = workout.Id,
                ExerciseId = exercise.Id,
                OrderIndex = workoutExerciseOrderIndex++
            });

            SeedExerciseSessions(dbContext, workout, user, exercise, sessionAt, startDate);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var seriesBuilder = new SeriesBuilder(dbContext);
        var plateauDetectionService = new PlateauDetectionService(seriesBuilder, dbContext);

        foreach (var exerciseId in affectedExerciseIds)
        {
            await plateauDetectionService.DetectAsync(user.Id, exerciseId, cancellationToken);
        }
    }

    private static void SeedExerciseSessions(
        OptiLiftsDbContext dbContext,
        Workout workout,
        User user,
        Exercise exercise,
        Func<int, (float Weight, int Reps, float Rpe)> sessionAt,
        DateTime startDate)
    {
        const int sessionCount = 24;
        const int setsPerSession = 3;

        for (var i = 0; i < sessionCount; i++)
        {
            var (weight, reps, rpe) = sessionAt(i);
            var sessionDate = startDate.AddDays(i * 7);

            var entry = new ScheduledEntry
            {
                WorkoutId = workout.Id,
                UserId = user.Id,
                Scheduled = sessionDate,
                Status = ScheduleStatus.Completed
            };
            dbContext.ScheduledEntries.Add(entry);

            var log = new WorkoutLog
            {
                EntryId = entry.Id,
                StartedAt = sessionDate,
                CompletedAt = sessionDate.AddMinutes(45),
                AiModified = false
            };
            dbContext.WorkoutLogs.Add(log);

            dbContext.WorkoutLogExercises.Add(new WorkoutLogExercise
            {
                LogId = log.Id,
                ExerciseId = exercise.Id,
                OrderIndex = 0,
                GroupNumber = 0
            });

            for (var setIdx = 0; setIdx < setsPerSession; setIdx++)
            {
                dbContext.WorkoutLogSets.Add(new WorkoutSetLog
                {
                    LogId = log.Id,
                    ExerciseId = exercise.Id,
                    Type = SetType.Normal,
                    Reps = reps,
                    Weight = weight,
                    GroupNumber = 0,
                    Rpe = rpe,
                    RestTime = 90,
                    OrderIndex = setIdx,
                    LoggedAt = sessionDate,
                    AiSuggested = false
                });
            }
        }
    }

    private static async Task SeedExercisesAsync(OptiLiftsDbContext dbContext, IBlobStorageService blobStorage, bool testing, CancellationToken cancellationToken)
    {
        if (await dbContext.Exercises.AnyAsync(cancellationToken))
        {
            return;
        }

        var muscleIds = await dbContext.Muscles.ToDictionaryAsync(m => m.Name, m => m.Id, cancellationToken);

        if (testing)
        {
            await IntegrationTestsSeeding(dbContext, muscleIds, cancellationToken);
            return;
        }

        var assembly = typeof(DatabaseSeeder).Assembly;
        using var stream = assembly.GetManifestResourceStream("OptiLifts.Infrastructure.Database.Seeders.exercises.json");

        if (stream == null)
        {
            throw new InvalidOperationException("Exercises.json not found");
        }

        using var reader = new StreamReader(stream);
        var jsonData = await reader.ReadToEndAsync(cancellationToken);

        var config = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var jsonExercises = JsonSerializer.Deserialize<List<JsonExercise>>(jsonData, config);

        if (jsonExercises == null)
        {
            return;
        }

        using var httpClient = new HttpClient(); //use this to get the images directly

        foreach (var exercise in jsonExercises)
        {
            if (!muscleIds.TryGetValue(exercise.PrimaryMuscle, out var primaryMuscleId))
            {
                continue; //so doesn't crash whole seeder if primary muscle doesn't match
            }

            string? imageUrl = null;
            if (!string.IsNullOrEmpty(exercise.ImageUrl))
            {
                try
                {
                    var response = await httpClient.GetByteArrayAsync(exercise.ImageUrl, cancellationToken);
                    using var imagestream = new MemoryStream(response);
                    var fileName = Path.GetFileName(new Uri(exercise.ImageUrl).LocalPath);
                    var imageName = $"{Guid.NewGuid()}-{fileName}";
                    imageUrl = await blobStorage.UploadFileAsync(imagestream, imageName, "image/jpeg", "exercises", cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to upload image for exercise {exercise.Name}: {ex.Message}");
                }


            }

            var exerciseType = ExerciseType.WeightReps;
            if (Enum.TryParse<ExerciseType>(exercise.ExerciseType, true, out var parsedType))
            {
                exerciseType = parsedType;
            }

            var exerciseEntry = new Exercise
            {
                Name = exercise.Name,
                Mechanic = exercise.Mechanic,
                Equipment = exercise.Equipment,
                ExerciseType = exerciseType,
                PrimaryMuscleId = primaryMuscleId,
                ImageUrl = imageUrl
            };
            dbContext.Exercises.Add(exerciseEntry);

            AddSecondaryMusclesAsync(dbContext, exerciseEntry, exercise.SecondaryMuscles, muscleIds);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

    }
}
