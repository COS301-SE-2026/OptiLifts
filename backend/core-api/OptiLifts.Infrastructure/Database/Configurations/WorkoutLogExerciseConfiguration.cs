using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class WorkoutLogExerciseConfiguration : IEntityTypeConfiguration<WorkoutLogExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutLogExercise> builder)
    {
        builder.ToTable("workout_log_exercises");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("log_exercise_id");

        builder.Property(w => w.LogId).HasColumnName("log_id").IsRequired();
        builder.Property(w => w.ExerciseId).HasColumnName("exercise_id").IsRequired();
        builder.Property(w => w.WorkoutExerciseId).HasColumnName("workout_exercise_id");
        builder.Property(w => w.OrderIndex).HasColumnName("order_index").IsRequired();
        builder.Property(w => w.GroupNumber).HasColumnName("group_number").IsRequired();

        builder.HasIndex(w => new { w.LogId, w.OrderIndex });
        builder.HasIndex(w => new { w.LogId, w.ExerciseId });
        builder.HasIndex(w => new { w.LogId, w.WorkoutExerciseId })
            .IsUnique()
            .HasFilter("workout_exercise_id IS NOT NULL");

        // Keep historical log exercises if the source template row is deleted.
        builder.HasOne<WorkoutLog>()
               .WithMany()
               .HasForeignKey(w => w.LogId)
               .OnDelete(DeleteBehavior.Cascade);

        // Exercise dictionary records are stable and remain referential.
        builder.HasOne<Exercise>()
               .WithMany()
               .HasForeignKey(w => w.ExerciseId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
