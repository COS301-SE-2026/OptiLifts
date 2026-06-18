using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("user_id");

        builder.Property(u => u.Email).HasColumnName("email").IsRequired();
        builder.Property(u => u.EmailHash).HasColumnName("email_hash").IsRequired().HasMaxLength(64);
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").IsRequired();
        builder.Property(u => u.DisplayName).HasColumnName("display_name").IsRequired().HasMaxLength(100);
        builder.Property(u => u.CreatedAt).HasColumnName("created_at").IsRequired();

        //creates a unique index on the email hash as it's deterministic
        builder.HasIndex(u => u.EmailHash).IsUnique();
    }
}