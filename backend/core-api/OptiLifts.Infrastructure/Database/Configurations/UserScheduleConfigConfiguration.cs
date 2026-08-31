using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OptiLifts.Domain.Users;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Security.Cryptography;

namespace OptiLifts.Infrastructure.Database.Configurations;

public class UserScheduleConfigConfiguration : IEntityTypeConfiguration<UserScheduleConfig>
{
    public void Configure(EntityTypeBuilder<UserScheduleConfig> builder)
    {
        builder.ToTable("user_schedule_config");
        builder.HasKey(c=> c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c=> c.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(c=> c.DynamicSchedulerEnabled).HasColumnName("dynamic_scheduler_enabled").IsRequired();
        builder.Property(c => c.MaxWorkoutsPerDay).HasColumnName("max_workouts_per_day").IsRequired();
        builder.Property(c => c.MinMuscleRestHours).HasColumnName("min_muscle_rest_hours").IsRequired();

        var restDaysComparer = new ValueComparer<List<string>>((c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2, c=> c.Aggregate(0, (a,v) => HashCode.Combine(a, v.GetHashCode())), c=> c.ToList());

        builder.Property(c => c.RestDays).HasColumnName("rest_day").HasConversion(v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), 
        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) 
        ?? new List<string>())
        .Metadata.SetValueComparer(restDaysComparer);
        
        builder.Property(c => c.CycleWindowLengthDays).HasColumnName("cycle_window_length_days").IsRequired();
        builder.Property(c => c.CycleStartDate).HasColumnName("cycle_start_date").IsRequired();

        builder.HasIndex(c => c.UserId).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);

    }
}