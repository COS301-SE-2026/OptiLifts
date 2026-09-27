using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ClashActivityKudosConfiguration : IEntityTypeConfiguration<ClashActivityKudos>
{
    public void Configure(EntityTypeBuilder<ClashActivityKudos> builder)
    {
        builder.ToTable("clash_activity_kudos");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("kudos_id");
        builder.Property(i => i.ActivityId).HasColumnName("activity_id").IsRequired();
        builder.Property(i => i.SenderUserId).HasColumnName("sender_user_id").IsRequired();
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<ClashActivity>().WithMany().HasForeignKey(i => i.ActivityId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(i => i.SenderUserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new
        {
            i.ActivityId,
            i.SenderUserId
        }).IsUnique();
    }
}