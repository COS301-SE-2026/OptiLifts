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
        builder.Property(pr => pr.PrType).HasColumnName("pr_type").HasConversion<string>().IsRequired();
        builder.Property(pr => pr.PrValue).HasColumnName("pr_value").IsRequired();
        builder.Property(pr => pr.AchievedWeight).HasColumnName("achieved_weight").IsRequired();
        builder.Property(pr => pr.AchievedReps).HasColumnName("achieved_reps").IsRequired();
        builder.Property(pr => pr.AchievedAt).HasColumnName("achieved_at").IsRequired();
        builder.Property(pr => pr.IsCurrent).HasColumnName("is_current").IsRequired();

        builder.HasIndex(pr => new { pr.UserId, pr.ExerciseId, pr.PrType })
            .IsUnique()
            .HasFilter("is_current = true");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(pr => pr.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Exercise>()
            .WithMany()
            .HasForeignKey(pr => pr.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}