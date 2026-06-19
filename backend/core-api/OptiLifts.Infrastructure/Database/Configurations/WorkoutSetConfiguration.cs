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
        builder.Property(s => s.WorkoutExerciseId).HasColumnName("workout_exercise_id").IsRequired();

        //EF core converts to an integer if left as an enum so rather make it a string 
        builder.Property(s => s.Type).HasColumnName("set_type").HasConversion<string>().IsRequired();
        builder.Property(s => s.Reps).HasColumnName("reps");
        builder.Property(s => s.Weight).HasColumnName("weight");
        builder.Property(s => s.Duration).HasColumnName("duration");
        builder.Property(s => s.Distance).HasColumnName("distance");
        builder.Property(s => s.OrderIndex).HasColumnName("order_index").IsRequired();
        builder.Property(s => s.RestTime).HasColumnName("rest_time").IsRequired();

        //FK relationship between workout set and workout exercise (N : 1)
        builder.HasOne<WorkoutExercise>()
               .WithMany()
               .HasForeignKey(s => s.WorkoutExerciseId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}