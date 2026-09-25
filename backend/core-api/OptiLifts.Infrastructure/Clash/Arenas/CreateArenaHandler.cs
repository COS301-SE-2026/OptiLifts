using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Commands;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class CreateArenaHandler : IRequestHandler<CreateArenaCommand, CreateArenaResult>
{
    private readonly OptiLiftsDbContext _db;
    private readonly IClashNotifier _notifier;

    public CreateArenaHandler(OptiLiftsDbContext db, IClashNotifier? notifier = null)
    {
        _db = db;
        _notifier = notifier ?? NullClashNotifier.Instance;
    }

    public async Task<CreateArenaResult> Handle(CreateArenaCommand request, CancellationToken cancellationToken)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return new CreateArenaResult(false, "Arena name cannot be empty.");
        }

        if (name.Length > 100)
        {
            return new CreateArenaResult(false, "Arena name cannot exceed 100 characters.");
        }

        var durationDays = request.DurationDays <= 0 ? 30 : Math.Clamp(request.DurationDays, 1, 365);
        var metricType = NormalizeMetricType(request.MetricType);

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null)
        {
            return new CreateArenaResult(false, "User not found.");
        }
        
        if (string.IsNullOrWhiteSpace(user.Weight) || !float.TryParse(user.Weight, NumberStyles.Any, CultureInfo.InvariantCulture, out var bw) || bw <= 0f)
        {
            return new CreateArenaResult(false, "Please set your bodyweight in your profile before creating a DOTS-based arena.");
        }

        // to generate a unique 6 char code
        string code;
        var attempts = 0;
        do
        {
            code = ArenaCodeHelper.GenerateCode(6);
            attempts++;
            if (attempts > 50)
            {
                code = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
                break;
            }
        } while (await _db.Arenas.AnyAsync(a => a.Code == code, cancellationToken));

        var now = DateTime.UtcNow;
        var arenaId = Guid.NewGuid().ToString();

        var arena = new Arena
        {
            Id = arenaId,
            Name = name,
            Type = "Private",
            Code = code,
            CreatedById = request.UserId,
            MetricType = metricType,
            DurationDays = durationDays,
            SeasonEndDate = now.AddDays(durationDays),
            CreatedAt = now
        };

        var member = new ArenaMember
        {
            ArenaId = arena.Id,
            UserId = request.UserId,
            Role = "Owner",
            JoinedAt = now
        };

        var activity = new ClashActivity
        {
            ArenaId = arena.Id,
            UserId = request.UserId,
            EventText = $"{user.DisplayName} created the squad!",
            Details = $"{arena.Name} launched for {durationDays} days.",
            IsPr = false,
            IsPromotion = false,
            KudosCount = 0,
            CreatedAt = now
        };

        _db.Arenas.Add(arena);
        _db.ArenaMembers.Add(member);
        _db.ClashActivities.Add(activity);

        await _db.SaveChangesAsync(cancellationToken);

        var activityDto = new ClashActivityDto(
            Id: activity.Id,
            ArenaId: arena.Id,
            UserId: user.Id,
            UserName: user.DisplayName,
            UserInitials: FriendshipHelpers.extractInitials(user.DisplayName),
            UserAvatarUrl: user.ProfileImageUrl,
            EventText: activity.EventText,
            Details: activity.Details,
            IsPr: activity.IsPr,
            IsPromotion: activity.IsPromotion,
            KudosCount: activity.KudosCount,
            HasUserKudoed: false,
            CreatedAt: activity.CreatedAt
        );

        await _notifier.BroadcastActivityAsync(arena.Id, activityDto, cancellationToken);

        var arenaDto = ClashFeedHelper.ToArenaDto(arena, memberCount: 1, userRole: "Owner", isUserMember: true);
        return new CreateArenaResult(true, "Arena created successfully.", arenaDto);
    }

    private static string NormalizeMetricType(string? metricType)
    {
        var clean = (metricType ?? string.Empty).Trim();
        return clean.ToLowerInvariant() switch
        {
            "totalvolume" or "volume" => "TotalVolume",
            "squate1rm" or "squat" => "SquatE1RM",
            "benche1rm" or "bench" => "BenchE1RM",
            "deadlifte1rm" or "deadlifts" or "deadlift" => "DeadliftE1RM",
            _ => "DotsOverall"
        };
    }
}
