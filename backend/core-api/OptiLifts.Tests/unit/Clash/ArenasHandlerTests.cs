using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Application.Clash;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Commands;
using OptiLifts.Application.Clash.Arenas.Queries;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Domain.Clash;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Clash.Arenas;
using OptiLifts.Infrastructure.Database;
using Xunit;

namespace OptiLifts.Tests.Unit.Clash;

public sealed class ArenasHandlerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly OptiLiftsDbContext _db;
    private readonly Mock<IClashNotifier> _notifierMock;

    public ArenasHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new OptiLiftsDbContext(options);
        _db.Database.EnsureCreated();
        _notifierMock = new Mock<IClashNotifier>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<User> SeedUserAsync(string name, string friendCode)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{friendCode.ToLowerInvariant()}@example.com",
            EmailHash = $"hash_{friendCode.ToLowerInvariant()}",
            PasswordHash = "test-hash",
            DisplayName = name,
            FriendCode = friendCode,
            Weight = "75.0"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task EstablishFriendshipAsync(Guid u1, Guid u2)
    {
        var (c1, c2) = FriendshipHelpers.toCanonOrder(u1, u2);
        _db.Friendships.Add(new Friendship
        {
            UserId1 = c1,
            UserId2 = c2,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }


    [Fact]
    public async Task CreateArena_ShouldSucceed_AndAssignOwnerRole()
    {
        var user = await SeedUserAsync("Alice", "ALC123");
        var handler = new CreateArenaHandler(_db, _notifierMock.Object);
        var command = new CreateArenaCommand("Iron Squad", "DotsOverall", 30, user.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Arena.Should().NotBeNull();
        result.Arena!.Name.Should().Be("Iron Squad");
        result.Arena.Code.Should().HaveLength(6);
        result.Arena.UserRole.Should().Be("Owner");
        result.Arena.MemberCount.Should().Be(1);

        var member = await _db.ArenaMembers.FirstOrDefaultAsync(m => m.ArenaId == result.Arena.Id && m.UserId == user.Id);
        member.Should().NotBeNull();
        member!.Role.Should().Be("Owner");

        var activity = await _db.ClashActivities.FirstOrDefaultAsync(a => a.ArenaId == result.Arena.Id);
        activity.Should().NotBeNull();
        activity!.EventText.Should().Contain("created the squad");

        _notifierMock.Verify(n => n.BroadcastActivityAsync(result.Arena.Id, It.IsAny<ClashActivityDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateArena_ShouldFail_WhenNameEmpty(string? name)
    {
        var user = await SeedUserAsync("Alice", "ALC124");
        var handler = new CreateArenaHandler(_db, _notifierMock.Object);
        var command = new CreateArenaCommand(name!, "DotsOverall", 30, user.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cannot be empty");
    }

    [Fact]
    public async Task CreateArena_ShouldFail_WhenNameExceeds100Chars()
    {
        var user = await SeedUserAsync("Alice", "ALC125");
        var handler = new CreateArenaHandler(_db, _notifierMock.Object);
        var command = new CreateArenaCommand(new string('X', 101), "DotsOverall", 30, user.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cannot exceed 100 characters");
    }

    [Fact]
    public async Task CreateArena_ShouldFail_WhenUserNotFound()
    {
        var handler = new CreateArenaHandler(_db, _notifierMock.Object);
        var command = new CreateArenaCommand("Ghost Squad", "DotsOverall", 30, Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("User not found");
    }


    [Fact]
    public async Task JoinArenaByCode_ShouldSucceed_WhenValidCode()
    {
        var owner = await SeedUserAsync("Alice", "ALC200");
        var joiner = await SeedUserAsync("Bob", "BOB200");

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Power Club", "TotalVolume", 14, owner.Id), CancellationToken.None);

        var handler = new JoinArenaByCodeHandler(_db, _notifierMock.Object);
        var joinResult = await handler.Handle(new JoinArenaByCodeCommand(createResult.Arena!.Code!, joiner.Id), CancellationToken.None);

        joinResult.Success.Should().BeTrue();
        joinResult.Arena.Should().NotBeNull();
        joinResult.Arena!.MemberCount.Should().Be(2);
        joinResult.Arena.UserRole.Should().Be("Member");

        var member = await _db.ArenaMembers.FirstOrDefaultAsync(m => m.ArenaId == createResult.Arena.Id && m.UserId == joiner.Id);
        member.Should().NotBeNull();
        member!.Role.Should().Be("Member");

        _notifierMock.Verify(n => n.BroadcastUserJoinedAsync(createResult.Arena.Id, joiner.DisplayName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task JoinArenaByCode_ShouldFail_WhenCodeNotFound()
    {
        var user = await SeedUserAsync("Charlie", "CHA201");
        var handler = new JoinArenaByCodeHandler(_db, _notifierMock.Object);

        var result = await handler.Handle(new JoinArenaByCodeCommand("NOPE99", user.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No arena found");
    }

    [Fact]
    public async Task JoinArenaByCode_ShouldFail_WhenAlreadyMember()
    {
        var user = await SeedUserAsync("Diana", "DIA202");
        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Fit Fam", "DotsOverall", 30, user.Id), CancellationToken.None);

        var handler = new JoinArenaByCodeHandler(_db, _notifierMock.Object);
        var result = await handler.Handle(new JoinArenaByCodeCommand(createResult.Arena!.Code!, user.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already a member");
    }

    [Fact]
    public async Task JoinArenaByCode_ShouldFail_WhenArenaExpired()
    {
        var owner = await SeedUserAsync("Elena", "ELE203");
        var joiner = await SeedUserAsync("Felix", "FEL203");

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Past Squad", "DotsOverall", 7, owner.Id), CancellationToken.None);

        var arena = await _db.Arenas.FirstAsync(a => a.Id == createResult.Arena!.Id);
        arena.SeasonEndDate = DateTime.UtcNow.AddDays(-1);
        await _db.SaveChangesAsync();

        var handler = new JoinArenaByCodeHandler(_db, _notifierMock.Object);
        var result = await handler.Handle(new JoinArenaByCodeCommand(arena.Code!, joiner.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("has ended");
    }


    [Fact]
    public async Task InviteFriendToArena_ShouldSucceed_WhenMutualFriends()
    {
        var user1 = await SeedUserAsync("George", "GEO300");
        var user2 = await SeedUserAsync("Hannah", "HAN300");
        await EstablishFriendshipAsync(user1.Id, user2.Id);

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Olympus", "SquatE1RM", 30, user1.Id), CancellationToken.None);

        var handler = new InviteFriendToArenaHandler(_db);
        var result = await handler.Handle(new InviteFriendToArenaCommand(Guid.Parse(createResult.Arena!.Id), user2.Id, user1.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.InviteId.Should().NotBeNull();

        var invite = await _db.ArenaInvites.FirstOrDefaultAsync(i => i.Id == result.InviteId);
        invite.Should().NotBeNull();
        invite!.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task InviteFriendToArena_ShouldFail_WhenNotMutualFriends()
    {
        var user1 = await SeedUserAsync("Ian", "IAN301");
        var user2 = await SeedUserAsync("Julia", "JUL301");

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Secret Squad", "DotsOverall", 30, user1.Id), CancellationToken.None);

        var handler = new InviteFriendToArenaHandler(_db);
        var result = await handler.Handle(new InviteFriendToArenaCommand(Guid.Parse(createResult.Arena!.Id), user2.Id, user1.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("mutual friends");
    }

    [Fact]
    public async Task InviteFriendToArena_ShouldFail_WhenInvitingSelf()
    {
        var user = await SeedUserAsync("Kevin", "KEV302");
        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Solo Arena", "DotsOverall", 30, user.Id), CancellationToken.None);

        var handler = new InviteFriendToArenaHandler(_db);
        var result = await handler.Handle(new InviteFriendToArenaCommand(Guid.Parse(createResult.Arena!.Id), user.Id, user.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("cannot invite yourself");
    }

    [Fact]
    public async Task InviteFriendToArena_ShouldFail_WhenFriendAlreadyMember()
    {
        var user1 = await SeedUserAsync("Liam", "LIA303");
        var user2 = await SeedUserAsync("Mia", "MIA303");
        await EstablishFriendshipAsync(user1.Id, user2.Id);

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Duo Gym", "DotsOverall", 30, user1.Id), CancellationToken.None);

        await new JoinArenaByCodeHandler(_db, _notifierMock.Object)
            .Handle(new JoinArenaByCodeCommand(createResult.Arena!.Code!, user2.Id), CancellationToken.None);

        var handler = new InviteFriendToArenaHandler(_db);
        var result = await handler.Handle(new InviteFriendToArenaCommand(Guid.Parse(createResult.Arena!.Id), user2.Id, user1.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already a member");
    }

    [Fact]
    public async Task InviteFriendToArena_ShouldFail_WhenInviteAlreadyPending()
    {
        var user1 = await SeedUserAsync("Noah", "NOA304");
        var user2 = await SeedUserAsync("Olivia", "OLI304");
        await EstablishFriendshipAsync(user1.Id, user2.Id);

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Repeat Squad", "DotsOverall", 30, user1.Id), CancellationToken.None);

        var handler = new InviteFriendToArenaHandler(_db);
        await handler.Handle(new InviteFriendToArenaCommand(Guid.Parse(createResult.Arena!.Id), user2.Id, user1.Id), CancellationToken.None);

        var duplicateResult = await handler.Handle(new InviteFriendToArenaCommand(Guid.Parse(createResult.Arena!.Id), user2.Id, user1.Id), CancellationToken.None);
        duplicateResult.Success.Should().BeFalse();
        duplicateResult.Message.Should().Contain("already sent");
    }


    [Fact]
    public async Task RespondToArenaInvite_Accept_ShouldAddUserAsMember()
    {
        var owner = await SeedUserAsync("Paul", "PAU400");
        var invitee = await SeedUserAsync("Quinn", "QUI400");
        await EstablishFriendshipAsync(owner.Id, invitee.Id);

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Invited Squad", "DotsOverall", 30, owner.Id), CancellationToken.None);

        var inviteResult = await new InviteFriendToArenaHandler(_db)
            .Handle(new InviteFriendToArenaCommand(Guid.Parse(createResult.Arena!.Id), invitee.Id, owner.Id), CancellationToken.None);

        var handler = new RespondToArenaInviteHandler(_db, _notifierMock.Object);
        var success = await handler.Handle(new RespondToArenaInviteCommand(inviteResult.InviteId!.Value, true, invitee.Id), CancellationToken.None);

        success.Should().BeTrue();

        var isMember = await _db.ArenaMembers.AnyAsync(m => m.ArenaId == createResult.Arena.Id && m.UserId == invitee.Id);
        isMember.Should().BeTrue();

        var invite = await _db.ArenaInvites.FirstAsync(i => i.Id == inviteResult.InviteId.Value);
        invite.Status.Should().Be("Accepted");
    }

    [Fact]
    public async Task RespondToArenaInvite_Decline_ShouldMarkRejectedWithoutAddingMember()
    {
        var owner = await SeedUserAsync("Rachel", "RAC401");
        var invitee = await SeedUserAsync("Sam", "SAM401");
        await EstablishFriendshipAsync(owner.Id, invitee.Id);

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Declined Squad", "DotsOverall", 30, owner.Id), CancellationToken.None);

        var inviteResult = await new InviteFriendToArenaHandler(_db)
            .Handle(new InviteFriendToArenaCommand(Guid.Parse(createResult.Arena!.Id), invitee.Id, owner.Id), CancellationToken.None);

        var handler = new RespondToArenaInviteHandler(_db, _notifierMock.Object);
        var success = await handler.Handle(new RespondToArenaInviteCommand(inviteResult.InviteId!.Value, false, invitee.Id), CancellationToken.None);

        success.Should().BeTrue();

        var isMember = await _db.ArenaMembers.AnyAsync(m => m.ArenaId == createResult.Arena.Id && m.UserId == invitee.Id);
        isMember.Should().BeFalse();

        var invite = await _db.ArenaInvites.FirstAsync(i => i.Id == inviteResult.InviteId.Value);
        invite.Status.Should().Be("Rejected");
    }


    [Fact]
    public async Task LeaveArena_ShouldSucceed_ForRegularMember()
    {
        var owner = await SeedUserAsync("Tom", "TOM500");
        var member = await SeedUserAsync("Uma", "UMA500");

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Depart Squad", "DotsOverall", 30, owner.Id), CancellationToken.None);

        await new JoinArenaByCodeHandler(_db, _notifierMock.Object)
            .Handle(new JoinArenaByCodeCommand(createResult.Arena!.Code!, member.Id), CancellationToken.None);

        var handler = new LeaveArenaHandler(_db, _notifierMock.Object);
        var result = await handler.Handle(new LeaveArenaCommand(Guid.Parse(createResult.Arena!.Id), member.Id), CancellationToken.None);

        result.Success.Should().BeTrue();

        var isStillMember = await _db.ArenaMembers.AnyAsync(m => m.ArenaId == createResult.Arena.Id && m.UserId == member.Id);
        isStillMember.Should().BeFalse();

        _notifierMock.Verify(n => n.BroadcastUserLeftAsync(createResult.Arena.Id, member.DisplayName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveArena_ShouldFail_ForOwner()
    {
        var owner = await SeedUserAsync("Victor", "VIC501");
        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Owner Squad", "DotsOverall", 30, owner.Id), CancellationToken.None);

        var handler = new LeaveArenaHandler(_db, _notifierMock.Object);
        var result = await handler.Handle(new LeaveArenaCommand(Guid.Parse(createResult.Arena!.Id), owner.Id), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.IsOwner.Should().BeTrue();
        result.Message.Should().Contain("Owners cannot leave");
    }


    [Fact]
    public async Task SendActivityKudos_ShouldIncrementKudos_AndBroadcast()
    {
        var user1 = await SeedUserAsync("Wendy", "WEN600");
        var user2 = await SeedUserAsync("Xavier", "XAV600");

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Kudos Club", "DotsOverall", 30, user1.Id), CancellationToken.None);

        var activity = await _db.ClashActivities.FirstAsync(a => a.ArenaId == createResult.Arena!.Id);

        var handler = new SendActivityKudosHandler(_db, _notifierMock.Object);
        var result = await handler.Handle(new SendActivityKudosCommand(activity.Id, user2.Id), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.KudosCount.Should().Be(1);

        var updatedActivity = await _db.ClashActivities.FirstAsync(a => a.Id == activity.Id);
        updatedActivity.KudosCount.Should().Be(1);

        _notifierMock.Verify(n => n.BroadcastKudosAsync(createResult.Arena!.Id, activity.Id, 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendActivityKudos_ShouldFail_WhenAlreadyKudoed()
    {
        var user1 = await SeedUserAsync("Yara", "YAR601");
        var user2 = await SeedUserAsync("Zack", "ZAC601");

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Cheer Squad", "DotsOverall", 30, user1.Id), CancellationToken.None);

        var activity = await _db.ClashActivities.FirstAsync(a => a.ArenaId == createResult.Arena!.Id);

        var handler = new SendActivityKudosHandler(_db, _notifierMock.Object);
        await handler.Handle(new SendActivityKudosCommand(activity.Id, user2.Id), CancellationToken.None);

        var duplicateResult = await handler.Handle(new SendActivityKudosCommand(activity.Id, user2.Id), CancellationToken.None);
        duplicateResult.Success.Should().BeFalse();
        duplicateResult.Message.Should().Contain("already cheered");
    }


    [Fact]
    public async Task AutoPruning_ShouldRetainOnly100LatestActivities()
    {
        var user = await SeedUserAsync("Pruner", "PRU700");
        var arenaId = Guid.NewGuid().ToString();
        _db.Arenas.Add(new Arena
        {
            Id = arenaId,
            Name = "Pruning Arena",
            Type = "Private",
            MetricType = "DotsOverall",
            DurationDays = 30,
            SeasonEndDate = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        });

        for (int i = 1; i <= 105; i++)
        {
            _db.ClashActivities.Add(new ClashActivity
            {
                Id = Guid.NewGuid(),
                ArenaId = arenaId,
                UserId = user.Id,
                EventText = $"Event {i}",
                Details = $"Detail {i}",
                CreatedAt = DateTime.UtcNow.AddSeconds(i)
            });
        }
        await _db.SaveChangesAsync();

        var countBefore = await _db.ClashActivities.CountAsync(a => a.ArenaId == arenaId);
        countBefore.Should().Be(105);

        await ClashFeedHelper.PruneArenaActivitiesAsync(_db, arenaId, CancellationToken.None);

        var countAfter = await _db.ClashActivities.CountAsync(a => a.ArenaId == arenaId);
        countAfter.Should().Be(100);

        // Oldest events (1 to 5) should be pruned, newest (105) should remain
        var remainingActivities = await _db.ClashActivities
            .Where(a => a.ArenaId == arenaId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        remainingActivities.First().EventText.Should().Be("Event 105");
        remainingActivities.Last().EventText.Should().Be("Event 6");
    }


    [Fact]
    public async Task GetUserArenas_ShouldReturnAllUserSquads()
    {
        var user = await SeedUserAsync("MultiArenaUser", "MUL800");

        var arena1 = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Squad One", "DotsOverall", 30, user.Id), CancellationToken.None);

        var arena2 = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Squad Two", "TotalVolume", 14, user.Id), CancellationToken.None);

        var handler = new GetUserArenasHandler(_db);
        var arenas = await handler.Handle(new GetUserArenasQuery(user.Id), CancellationToken.None);

        arenas.Should().HaveCount(2);
        arenas.Select(a => a.Name).Should().Contain(new[] { "Squad One", "Squad Two" });
    }

    [Fact]
    public async Task GetArenaLeaderboard_ShouldCalculateStandings()
    {
        var user1 = await SeedUserAsync("Leader", "LEA900");
        var user2 = await SeedUserAsync("RunnerUp", "RUN900");

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Ranked Squad", "DotsOverall", 30, user1.Id), CancellationToken.None);

        await new JoinArenaByCodeHandler(_db, _notifierMock.Object)
            .Handle(new JoinArenaByCodeCommand(createResult.Arena!.Code!, user2.Id), CancellationToken.None);

        var seasonKey = DateTime.UtcNow.ToString("yyyy-MM");

        _db.AthleteSeasonSnapshots.AddRange(
            new AthleteSeasonSnapshot
            {
                UserId = user1.Id,
                SeasonKey = seasonKey,
                DisplayName = user1.DisplayName,
                DotsScore = 400.0m,
                TotalE1RM = 500m,
                Tier = "Diamond"
            },
            new AthleteSeasonSnapshot
            {
                UserId = user2.Id,
                SeasonKey = seasonKey,
                DisplayName = user2.DisplayName,
                DotsScore = 320.0m,
                TotalE1RM = 420m,
                Tier = "Gold"
            }
        );
        await _db.SaveChangesAsync();

        var handler = new GetArenaLeaderboardHandler(_db);
        var result = await handler.Handle(new GetArenaLeaderboardQuery(Guid.Parse(createResult.Arena!.Id), user1.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Standings.Should().HaveCount(2);
        result.Standings[0].UserId.Should().Be(user1.Id);
        result.Standings[0].Rank.Should().Be(1);
        result.Standings[0].Score.Should().Be(400.0m);
        result.Standings[1].UserId.Should().Be(user2.Id);
        result.Standings[1].Rank.Should().Be(2);
        result.Standings[1].Score.Should().Be(320.0m);
        result.UserStanding.Should().NotBeNull();
        result.UserStanding!.UserId.Should().Be(user1.Id);
    }

    [Fact]
    public async Task GetArenaFeed_ShouldReturnActivitiesWithKudosStatus()
    {
        var user1 = await SeedUserAsync("Poster", "POS901");
        var user2 = await SeedUserAsync("Cheerer", "CHE901");

        var createResult = await new CreateArenaHandler(_db, _notifierMock.Object)
            .Handle(new CreateArenaCommand("Feed Squad", "DotsOverall", 30, user1.Id), CancellationToken.None);

        var activity = await _db.ClashActivities.FirstAsync(a => a.ArenaId == createResult.Arena!.Id);

        await new SendActivityKudosHandler(_db, _notifierMock.Object)
            .Handle(new SendActivityKudosCommand(activity.Id, user2.Id), CancellationToken.None);

        var handler = new GetArenaFeedHandler(_db);
        var feedForUser2 = await handler.Handle(new GetArenaFeedQuery(Guid.Parse(createResult.Arena!.Id), user2.Id), CancellationToken.None);

        feedForUser2.Should().HaveCount(1);
        feedForUser2[0].KudosCount.Should().Be(1);
        feedForUser2[0].HasUserKudoed.Should().BeTrue();

        var feedForUser1 = await handler.Handle(new GetArenaFeedQuery(Guid.Parse(createResult.Arena!.Id), user1.Id), CancellationToken.None);
        feedForUser1[0].HasUserKudoed.Should().BeFalse();
    }
}
