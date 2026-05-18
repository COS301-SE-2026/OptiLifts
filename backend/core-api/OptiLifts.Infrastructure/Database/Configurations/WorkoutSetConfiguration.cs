using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class WorkoutSetConfiguration : IEntityTypeConfiguration<WorkoutSet>
{
    public void Configure(EntityTypeBuilder<WorkoutSet> builder)
    {
        builder.ToTable("sets");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("set_id");
        builder.Property(s => s.WorkoutId).HasColumnName("workout_id").IsRequired();
        builder.Property(s => s.ExerciseId).HasColumnName("exercise_id").IsRequired();
        
        //EF core converts to an integer if left as an enum so rather make it a string 
        builder.Property(s => s.Type).HasColumnName("set_type").HasConversion<string>().IsRequired();
        
        builder.Property(s => s.Reps).HasColumnName("reps").IsRequired();
        builder.Property(s => s.Weight).HasColumnName("weight").IsRequired();
        builder.Property(s => s.OrderIndex).HasColumnName("order_index").IsRequired();
        builder.Property(s => s.RestTime).HasColumnName("rest_time").IsRequired();

        //FK relationship between workout set and workout
        builder.HasOne<Workout>()
               .WithMany()
               .HasForeignKey(s => s.WorkoutId)
               .OnDelete(DeleteBehavior.Cascade);

        //FK relationship between workout set and exercise
        builder.HasOne<Exercise>()
               .WithMany()
               .HasForeignKey(s => s.ExerciseId)
               .OnDelete(DeleteBehavior.Restrict); //restrict prevents the exercises from being deleted
    }
}