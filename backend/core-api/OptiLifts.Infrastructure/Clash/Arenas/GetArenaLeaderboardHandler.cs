using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Queries;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Clash.Arenas;

public sealed class GetArenaLeaderboardHandler : IRequestHandler<GetArenaLeaderboardQuery, ArenaLeaderboardResult?>
{
    private readonly OptiLiftsDbContext _db;

    public GetArenaLeaderboardHandler(OptiLiftsDbContext db)
    {
        _db = db;
    }

    public async Task<ArenaLeaderboardResult?> Handle(GetArenaLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var arenaIdStr = !string.IsNullOrWhiteSpace(request.ArenaStringId)
            ? request.ArenaStringId
            : request.ArenaId.ToString();

        var arena = await _db.Arenas.AsNoTracking().FirstOrDefaultAsync(a => a.Id == arenaIdStr, cancellationToken);
        if (arena is null)
        {
            return null;
        }

        var callerMembership = await _db.ArenaMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ArenaId == arena.Id && m.UserId == request.UserId, cancellationToken);

        var memberCount = await _db.ArenaMembers
            .CountAsync(m => m.ArenaId == arena.Id, cancellationToken);

        var arenaDto = ClashFeedHelper.ToArenaDto(
            arena,
            memberCount,
            callerMembership?.Role,
            callerMembership != null
        );

        var seasonKey = DateTime.UtcNow.ToString("yyyy-MM");

        if (string.Equals(arena.Type, "Private", StringComparison.OrdinalIgnoreCase))
        {
            var members = await _db.ArenaMembers
                .AsNoTracking()
                .Where(m => m.ArenaId == arena.Id)
                .ToListAsync(cancellationToken);

            var userIds = members.Select(m => m.UserId).Distinct().ToList();
            var users = await _db.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, cancellationToken);

            var snapshots = await _db.AthleteSeasonSnapshots
                .AsNoTracking()
                .Where(s => userIds.Contains(s.UserId) && s.SeasonKey == seasonKey)
                .ToDictionaryAsync(s => s.UserId, cancellationToken);

            var standingsList = new List<ArenaLeaderboardEntryDto>();

            foreach (var member in members)
            {
                if (!users.TryGetValue(member.UserId, out var user))
                {
                    continue;
                }

                snapshots.TryGetValue(member.UserId, out var snap);

                var dots = snap?.DotsScore ?? 0m;
                var squat = snap?.Squat1RM ?? 0m;
                var bench = snap?.Bench1RM ?? 0m;
                var deadlift = snap?.Deadlift1RM ?? 0m;
                var totalE1rm = snap?.TotalE1RM ?? 0m;
                var volume = snap?.WeeklyVolumeKg ?? 0m;
                var tier = snap?.Tier ?? "Bronze";
                var tierLevel = snap?.TierLevel ?? 1;
                var trend = snap?.RankTrend ?? 0;
                var gender = snap?.Gender ?? (user.Sex ?? "Male");
                var bodyweight = snap?.BodyweightKg ?? 75.0m;

                var score = arena.MetricType switch
                {
                    "TotalVolume" => volume,
                    "SquatE1RM" => squat,
                    "BenchE1RM" => bench,
                    "DeadliftE1RM" => deadlift,
                    _ => dots
                };

                standingsList.Add(new ArenaLeaderboardEntryDto(
                    Rank: 0,
                    UserId: user.Id,
                    DisplayName: user.DisplayName,
                    Initials: FriendshipHelpers.extractInitials(user.DisplayName),
                    AvatarUrl: user.ProfileImageUrl,
                    Gender: gender,
                    BodyweightKg: bodyweight,
                    Score: score,
                    DotsScore: dots,
                    TotalE1RM: totalE1rm,
                    Squat1RM: squat,
                    Bench1RM: bench,
                    Deadlift1RM: deadlift,
                    WeeklyVolumeKg: volume,
                    Tier: tier,
                    TierLevel: tierLevel,
                    Trend: trend,
                    Role: member.Role,
                    IsCurrentUser: user.Id == request.UserId
                ));
            }

            var orderedStandings = standingsList
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.DisplayName)
                .Select((s, index) => s with { Rank = index + 1 })
                .ToList();

            var userStanding = orderedStandings.FirstOrDefault(s => s.UserId == request.UserId);
            return new ArenaLeaderboardResult(arenaDto, orderedStandings, userStanding);
        }
        else
        {
            // System public / divisional league
            var publicSnapshots = await _db.AthleteSeasonSnapshots
                .AsNoTracking()
                .Where(s => s.SeasonKey == seasonKey && s.IsOptedIn)
                .OrderByDescending(s => s.DotsScore)
                .Take(100)
                .ToListAsync(cancellationToken);

            var standings = publicSnapshots.Select((s, index) => new ArenaLeaderboardEntryDto(
                Rank: index + 1,
                UserId: s.UserId,
                DisplayName: s.DisplayName,
                Initials: FriendshipHelpers.extractInitials(s.DisplayName),
                AvatarUrl: s.AvatarUrl,
                Gender: s.Gender,
                BodyweightKg: s.BodyweightKg,
                Score: s.DotsScore,
                DotsScore: s.DotsScore,
                TotalE1RM: s.TotalE1RM,
                Squat1RM: s.Squat1RM,
                Bench1RM: s.Bench1RM,
                Deadlift1RM: s.Deadlift1RM,
                WeeklyVolumeKg: s.WeeklyVolumeKg,
                Tier: s.Tier,
                TierLevel: s.TierLevel,
                Trend: s.RankTrend,
                Role: "Member",
                IsCurrentUser: s.UserId == request.UserId
            )).ToList();

            var userStanding = standings.FirstOrDefault(s => s.UserId == request.UserId);
            return new ArenaLeaderboardResult(arenaDto, standings, userStanding);
        }
    }
}
