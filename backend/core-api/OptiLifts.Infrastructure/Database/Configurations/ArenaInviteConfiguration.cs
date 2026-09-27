using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class ArenaInviteConfiguration : IEntityTypeConfiguration<ArenaInvite>
{
    public void Configure(EntityTypeBuilder<ArenaInvite> builder)
    {
        builder.ToTable("arena_invites");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("invite_id");
        builder.Property(i => i.ArenaId).HasColumnName("arena_id").HasMaxLength(50).IsRequired();
        builder.Property(i => i.InvitedByUserId).HasColumnName("invited_by_user_id").IsRequired();
        builder.Property(i => i.InvitedUserId).HasColumnName("invited_user_id").IsRequired();
        builder.Property(i => i.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasOne<Arena>().WithMany().HasForeignKey(i => i.ArenaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(i => i.InvitedByUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(i => i.InvitedUserId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new
        {
            i.InvitedUserId,
            i.Status
        });
    }
}