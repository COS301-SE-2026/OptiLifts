using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("exercises");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("exercise_id");

        builder.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
        builder.Property(e => e.Mechanic).HasColumnName("mechanic");
        builder.Property(e => e.Equipment).HasColumnName("equipment");
        builder.Property(e => e.Category).HasColumnName("category").IsRequired();

        //arrays for now, we need to discuss if we should keep them vs make a new table
        builder.Property(e => e.PrimaryMuscles).HasColumnName("primary_muscles");
        builder.Property(e => e.SecondaryMuscles).HasColumnName("secondary_muscles");
    }
}