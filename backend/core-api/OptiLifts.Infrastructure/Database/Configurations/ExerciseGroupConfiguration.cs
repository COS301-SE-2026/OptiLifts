using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ExerciseGroupConfiguration : IEntityTypeConfiguration<ExerciseGroup>
{
    public void Configure(EntityTypeBuilder<ExerciseGroup> builder)
    {
        builder.ToTable("exercise_groups");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasColumnName("exercise_group_id");
        builder.Property(g => g.WorkoutId).HasColumnName("workout_id").IsRequired();

        builder.Property(g => g.Type).HasColumnName("group_type").HasConversion<string>().IsRequired();
        builder.Property(g => g.Rounds).HasColumnName("rounds").IsRequired();
        builder.Property(g => g.RestTime).HasColumnName("rest_time").IsRequired();

        builder.HasOne<Workout>()
               .WithMany()
               .HasForeignKey(g => g.WorkoutId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}