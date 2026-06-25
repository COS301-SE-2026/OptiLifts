using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class SecMuscleConfiguration : IEntityTypeConfiguration<SecMuscle>
{
    public void Configure(EntityTypeBuilder<SecMuscle> builder)
    {
        builder.ToTable("sec_muscles");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("sec_muscle_id");
        builder.Property(s => s.MuscleId).HasColumnName("muscle_id").IsRequired();
        builder.Property(s => s.ExerciseId).HasColumnName("exercise_id").IsRequired();

        //foreign key to muscle that is being referenced
        builder.HasOne<Muscle>()
                .WithMany()
                .HasForeignKey(s => s.MuscleId)
                .OnDelete(DeleteBehavior.Cascade);

        //foreign key to exercise that is being referenced
        builder.HasOne<Exercise>()
                .WithMany()
                .HasForeignKey(s => s.ExerciseId)
                .OnDelete(DeleteBehavior.Cascade);
    }
}