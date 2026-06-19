using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using OptiLifts.Domain.Common;
using OptiLifts.Domain.Messaging;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Security;



namespace OptiLifts.Infrastructure.Database;

public class OptiLiftsDbContext : DbContext
{
    private readonly IEncryptionProvider _encryptionProvider;
    public OptiLiftsDbContext(DbContextOptions<OptiLiftsDbContext> options, IConfiguration? configuration = null) : base(options)
    {
        //default key for testing
        var key = configuration?["DB_ENCRYPTION_KEY"] ?? "+8bGaoOpx4CEfxnMcX1RG2qrcJaT+RZO/0IIpSePZQA=";
        if (string.IsNullOrEmpty(key))
        {
            throw new InvalidOperationException("Database encryption key is missing");
        }

        _encryptionProvider = new AesEncryptionProvider(key);
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Folder> Folders { get; set; }
    public DbSet<Workout> Workouts { get; set; }
    public DbSet<Exercise> Exercises { get; set; }
    public DbSet<WorkoutSet> Sets { get; set; }
    public DbSet<WorkoutLog> WorkoutLogs { get; set; }
    public DbSet<WorkoutSetLog> WorkoutLogSets { get; set; }
    public DbSet<Muscle> Muscles { get; set; }
    public DbSet<SecMuscle> SecMuscles { get; set; }
    public DbSet<WorkoutExercise> WorkoutExercises { get; set; }
    public DbSet<ScheduledEntry> ScheduledEntries { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<UserModel> UserModels { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OptiLiftsDbContext).Assembly);

        var encrypter = new ValueConverter<string, string>(
            plainT => _encryptionProvider.Encrypt(plainT),
            cipherT => _encryptionProvider.Decrypt(cipherT)
        );

        for (int i = 0; i < modelBuilder.Model.GetEntityTypes().Count(); i++)
        {
            var entity = modelBuilder.Model.GetEntityTypes().ElementAt(i);
            var properties = entity.ClrType.GetProperties()
            .Where(p => Attribute.IsDefined(p, typeof(EncryptedAttribute)));

            for (int j = 0; j < properties.Count(); j++)
            {
                var prop = properties.ElementAt(j);
                modelBuilder.Entity(entity.Name).Property(prop.Name).HasConversion(encrypter);
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
