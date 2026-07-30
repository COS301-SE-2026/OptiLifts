using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class WorkoutExerciseConfiguration : IEntityTypeConfiguration<WorkoutExercise>
{
    public void Configure(EntityTypeBuilder<WorkoutExercise> builder)
    {
        builder.ToTable("workout_exercises");

        builder.HasKey(we => we.Id);
        builder.Property(we => we.Id).HasColumnName("workout_exercise_id");

        builder.Property(we => we.WorkoutId).HasColumnName("workout_id").IsRequired();
        builder.Property(we => we.ExerciseId).HasColumnName("exercise_dict_id").IsRequired();
        builder.Property(we => we.GroupId).HasColumnName("group_id");
        builder.Property(we => we.OrderIndex).HasColumnName("order_index").IsRequired();

        //FK relationship between WE and Workout
        builder.HasOne<Workout>()
               .WithMany()
               .HasForeignKey(we => we.WorkoutId)
               .OnDelete(DeleteBehavior.Cascade);

        //FK relationship between WE and exercise_dictionary
        builder.HasOne<Exercise>()
                .WithMany()
                .HasForeignKey(we => we.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

        //FK between WE and ExerciseGroup
        builder.HasOne<ExerciseGroup>()
                .WithMany()
                .HasForeignKey(we => we.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
    }
}