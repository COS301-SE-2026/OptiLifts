using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ExercisePrConfiguration : IEntityTypeConfiguration<ExercisePr>
{
    public void Configure(EntityTypeBuilder<ExercisePr> builder)
    {
        builder.ToTable("exercise_prs");

        builder.HasKey(pr => pr.Id);
        builder.Property(pr => pr.Id).HasColumnName("pr_id");

        builder.Property(pr => pr.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(pr => pr.ExerciseId).HasColumnName("exercise_id").IsRequired();
        builder.Property(pr => pr.WorkoutLogSetId).HasColumnName("workout_log_set_id").IsRequired();
        builder.Property(pr => pr.PrType).HasColumnName("pr_type").HasConversion<string>().IsRequired();
        builder.Property(pr => pr.PrValue).HasColumnName("pr_value").IsRequired();
        builder.Property(pr => pr.AchievedWeight).HasColumnName("achieved_weight").IsRequired();
        builder.Property(pr => pr.AchievedReps).HasColumnName("achieved_reps").IsRequired();

        builder.HasIndex(pr => new { pr.UserId, pr.ExerciseId, pr.PrType });
        builder.HasIndex(pr => pr.WorkoutLogSetId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(pr => pr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(pr => pr.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<WorkoutSetLog>()
            .WithMany()
            .HasForeignKey(pr => pr.WorkoutLogSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}