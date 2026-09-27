using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.ToTable("friend_requests");

        builder.HasKey(fr => fr.Id);
        builder.Property(fr => fr.Id).HasColumnName("friend_request_id");
        builder.Property(fr => fr.SenderId).HasColumnName("sender_id").IsRequired();
        builder.Property(fr => fr.ReceiverId).HasColumnName("receiver_id").IsRequired();
        builder.Property(fr => fr.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("Pending").IsRequired();
        builder.Property(fr => fr.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(fr => fr.RespondedAt).HasColumnName("responded_at").IsRequired(false);

        builder.HasOne<User>().WithMany().HasForeignKey(fr => fr.SenderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(fr => fr.ReceiverId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(fr => new
        {
            fr.ReceiverId,
            fr.Status
        });
        builder.HasIndex(fr => new
        {
            fr.SenderId,
            fr.ReceiverId
        }).IsUnique().HasFilter("status = 'Pending'");

    }
}