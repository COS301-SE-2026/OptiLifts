using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class UserModelConfiguration : IEntityTypeConfiguration<UserModel>
{
    public void Configure(EntityTypeBuilder<UserModel> builder)
    {
        builder.ToTable("user_models");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("model_id");

        builder.Property(u => u.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(u => u.ModelPath).HasColumnName("model_path").IsRequired();
        builder.Property(u => u.TrainingSessions).HasColumnName("training_sessions").IsRequired();
        builder.Property(u => u.TrainedAt).HasColumnName("trained_at").IsRequired();

        builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(u => u.UserId)
                .OnDelete(DeleteBehavior.Cascade);
    }
}