using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Workouts;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class MuscleConfiguration : IEntityTypeConfiguration<Muscle>
{
    public void Configure(EntityTypeBuilder<Muscle> builder)
    {
        builder.ToTable("muscles");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("muscle_id");
        builder.Property(m => m.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
    }
}