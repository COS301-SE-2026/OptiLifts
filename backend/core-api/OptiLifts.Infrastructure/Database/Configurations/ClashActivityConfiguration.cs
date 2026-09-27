using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ClashActivityConfiguration : IEntityTypeConfiguration<ClashActivity>
{
    public void Configure(EntityTypeBuilder<ClashActivity> builder)
    {
        builder.ToTable("clash_activities");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("activity_id");
        builder.Property(i => i.ArenaId).HasColumnName("arena_id").HasMaxLength(50).IsRequired();
        builder.Property(i => i.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(i => i.EventText).HasColumnName("event_text").HasMaxLength(150).IsRequired();
        builder.Property(i => i.Details).HasColumnName("details").HasMaxLength(255).IsRequired();
        builder.Property(i => i.IsPr).HasColumnName("is_pr").HasDefaultValue(false);
        builder.Property(i => i.IsPromotion).HasColumnName("is_promotion").HasDefaultValue(false);
        builder.Property(i => i.KudosCount).HasColumnName("kudos_count").HasDefaultValue(0);
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<Arena>().WithMany().HasForeignKey(i => i.ArenaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new
        {
            i.ArenaId,
            i.CreatedAt
        });
    }
}