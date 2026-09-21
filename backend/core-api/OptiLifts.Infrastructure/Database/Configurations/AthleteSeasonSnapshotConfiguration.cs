using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class AthleteSeasonSnapshotConfiguration : IEntityTypeConfiguration<AthleteSeasonSnapshot>
{
    public void Configure(EntityTypeBuilder<AthleteSeasonSnapshot> builder)
    {
        builder.ToTable("athlete_season_snapshots");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("snapshot_id");
        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(s => s.SeasonKey).HasColumnName("season_key").HasMaxLength(7).IsRequired();
        builder.Property(s => s.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
        builder.Property(s => s.AvatarUrl).HasColumnName("avatar_url").HasMaxLength(500);
        builder.Property(s => s.Gender).HasColumnName("gender").HasMaxLength(10).IsRequired();
        builder.Property(s => s.BodyweightKg).HasColumnName("bodyweight_kg").HasPrecision(5,2).IsRequired();
        builder.Property(s => s.IsOptedIn).HasColumnName("is_opted_in").HasDefaultValue(false);
        builder.Property(s => s.Squat1RM).HasColumnName("squat_1rm").HasPrecision(6, 2).HasDefaultValue(0m);
        builder.Property(s => s.Bench1RM).HasColumnName("bench_1rm").HasPrecision(6, 2).HasDefaultValue(0m);
        builder.Property(s => s.Deadlift1RM).HasColumnName("deadlift_1rm").HasPrecision(6, 2).HasDefaultValue(0m);
        builder.Property(s => s.TotalE1RM).HasColumnName("total_e1rm").HasPrecision(7, 2).HasDefaultValue(0m);
        builder.Property(s => s.DotsScore).HasColumnName("dots_score").HasPrecision(6, 2).HasDefaultValue(0m);
        builder.Property(s => s.Tier).HasColumnName("tier").HasMaxLength(30).HasDefaultValue("Bronze");
        builder.Property(s => s.TierLevel).HasColumnName("tier_level").HasDefaultValue(1);
        builder.Property(s => s.RankTrend).HasColumnName("rank_trend").HasDefaultValue(0);
        builder.Property(s => s.WeeklyVolumeKg).HasColumnName("weekly_volume_kg").HasPrecision(10, 2).HasDefaultValue(0m);
        builder.Property(s => s.LastWorkoutDate).HasColumnName("last_workout_date").HasColumnType("date").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new
        {
            s.UserId,
            s.SeasonKey
        }).IsUnique();
        builder.HasIndex(s => new
        {
            s.SeasonKey,
            s.IsOptedIn,
            s.Gender,
            s.BodyweightKg,
            s.DotsScore
        });

        builder.HasIndex(s => new { s.SeasonKey, s.WeeklyVolumeKg });
        builder.HasIndex(s => new { s.SeasonKey, s.Squat1RM });
        builder.HasIndex(s => new { s.SeasonKey, s.Bench1RM });
        builder.HasIndex(s => new { s.SeasonKey, s.Deadlift1RM });
    }
}