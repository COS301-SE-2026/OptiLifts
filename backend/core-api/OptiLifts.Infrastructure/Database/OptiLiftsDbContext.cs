using Microsoft.EntityFrameworkCore;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;

using OptiLifts.Infrastructure.Security;
using Microsoft.Extensions.Configuration;

using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OptiLifts.Domain.Common;



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
