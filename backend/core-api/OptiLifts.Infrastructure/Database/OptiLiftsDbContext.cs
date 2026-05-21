using Microsoft.EntityFrameworkCore;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;


namespace OptiLifts.Infrastructure.Database;

public class OptiLiftsDbContext : DbContext
{
    public OptiLiftsDbContext(DbContextOptions<OptiLiftsDbContext> options) : base(options) { }

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
        base.OnModelCreating(modelBuilder);
    }
}
