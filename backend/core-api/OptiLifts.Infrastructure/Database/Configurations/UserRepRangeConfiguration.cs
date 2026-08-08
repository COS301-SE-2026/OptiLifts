using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class UserRepRangeConfiguration : IEntityTypeConfiguration<UserRepRange>
{
    public void Configure(EntityTypeBuilder<UserRepRange> builder)
    {
        builder.ToTable("user_rep_range", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_user_rep_range_exercise_type", "exercise_type IN ('Compound', 'Isolation')");
            tableBuilder.HasCheckConstraint("CK_user_rep_range_bounds", "lower_limit <= upper_limit");
        });

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("rep_range_id");

        builder.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(r => r.ExerciseType).HasColumnName("exercise_type").HasConversion<string>().IsRequired();
        builder.Property(r => r.LowerLimit).HasColumnName("lower_limit").IsRequired().HasDefaultValue(8);
        builder.Property(r => r.UpperLimit).HasColumnName("upper_limit").IsRequired().HasDefaultValue(10);

        builder.HasIndex(r => new { r.UserId, r.ExerciseType }).IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
