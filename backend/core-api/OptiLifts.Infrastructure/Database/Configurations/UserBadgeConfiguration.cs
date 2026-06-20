using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Gamification;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class UserBadgeConfiguration : IEntityTypeConfiguration<UserBadge>
{
    public void Configure(EntityTypeBuilder<UserBadge> builder)
    {
        builder.ToTable("user_badges");

        builder.HasKey(ub => ub.Id);
        builder.Property(ub => ub.Id).HasColumnName("user_badge_id");

        builder.Property(ub => ub.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(ub => ub.BadgeId).HasColumnName("badge_id").IsRequired();
        builder.Property(ub => ub.EarnedAt).HasColumnName("earned_at").IsRequired();

        builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(ub => ub.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Badge>()
                .WithMany()
                .HasForeignKey(ub => ub.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ub => new { ub.UserId, ub.BadgeId }).IsUnique();
    }
}