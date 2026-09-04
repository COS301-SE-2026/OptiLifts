using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ExerciseEstimationConfiguration : IEntityTypeConfiguration<ExerciseEstimation>
{
    public void Configure(EntityTypeBuilder<ExerciseEstimation> builder)
    {
        builder.ToTable("exercise_estimation");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("estimate_id");

        builder.Property(e => e.ExerciseId).HasColumnName("exercise_dict_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.Weight).HasColumnName("weight");
        builder.Property(e => e.Reps).HasColumnName("reps").IsRequired();
        builder.Property(e => e.ExerciseType).HasColumnName("exercise_type").HasConversion<string>().IsRequired();
        builder.Property(e => e.TimeStamp).HasColumnName("time_stamp").IsRequired();

        builder.HasIndex(e => new { e.UserId, e.ExerciseId, e.TimeStamp });

        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(e => e.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
