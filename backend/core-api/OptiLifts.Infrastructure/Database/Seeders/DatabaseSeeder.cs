using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Storage;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Security;

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
