using System.Text;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Storage;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Friends.Commands;
using OptiLifts.Application.Clash.Friends.Queries;
using OptiLifts.Infrastructure.Clash.Friends;
using Xunit;

namespace OptiLifts.Tests.Unit.Clash;

//avoiding sonarqube (the evil one) at all costs once again aka geen duplication
public sealed class FriendsHandlerTests : IDisposable
{
    private const string code1 = "ABC123";
    private const string code2 = "BCD123";
    private const string code3 = "CDE123";
    private const string passwordHash = "test-hash";

    private readonly SqliteConnection _connection;
    private readonly OptiLiftsDbContext _db;

    public FriendsHandlerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(_connection).Options;
        _db = new OptiLiftsDbContext(options);
        _db.Database.EnsureCreated();
    }
    public void Dispose()//do i really need this?
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<User> SeedUserAsync(string name, string code)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"{code.ToLowerInvariant()}@example.com",
            EmailHash = $"hash_{code.ToLowerInvariant()}",
            PasswordHash = passwordHash,
            DisplayName = name,
            FriendCode = code
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    //SendFriendRequestHandler tests - all passing
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SendFriendRequest_ShouldFail_WhenCodeEmpty(string code)
    {
        var user = await SeedUserAsync("Alice", code1);
        var handler = new SendFriendRequestHandler(_db);        
        var result = await handler.Handle(new SendFriendRequestCommand(user.Id, code), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Friend code cannot be empty");
    }
    [Fact]
    public async Task SendFriendRequest_SHouldFail_WhenUserNotFound()
    {
        var user = await SeedUserAsync("Dany", code1);
        var handler = new SendFriendRequestHandler(_db);
        var result = await handler.Handle(new SendFriendRequestCommand(user.Id, "NOTEXIST"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("No user found with that friend code");
    }

    [Fact]
    public async Task SendFriendRequest_SHouldFail_WhenAlreadyFriends()
    {
        var user = await SeedUserAsync("Dany", code1);
        var user2 = await SeedUserAsync("Drogo", code2);
        var (u1, u2) = FriendshipHelpers.toCanonOrder(user.Id, user2.Id);
        _db.Friendships.Add(new Friendship
        {
            UserId1 = u1,
            UserId2= u2,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var handler = new SendFriendRequestHandler(_db);
        var result = await handler.Handle(new SendFriendRequestCommand(user.Id, code2), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("You are already friends with this user");
    }

    [Fact]
    public async Task SendFriendRequest_SHouldFail_WhenRequestArliPending()
    {
        var user = await SeedUserAsync("Dany", code1);
        var user2 = await SeedUserAsync("Viserys", code2);
        _db.FriendRequests.Add(new FriendRequest
        {
            SenderId = user.Id,
            ReceiverId = user2.Id,
            Status = FriendshipHelpers.statusPending,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var handler = new SendFriendRequestHandler(_db);
        var result = await handler.Handle(new SendFriendRequestCommand(user.Id, code2), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("You have already sent a friend request to this user");
    }

    [Fact]
    public async Task SendFriendRequest_SHouldSucceed_WhenValid()
    {
        var user = await SeedUserAsync("Dany", code1);
        var user2 = await SeedUserAsync("Ionos", code2);

        var handler = new SendFriendRequestHandler(_db);
        var result = await handler.Handle(new SendFriendRequestCommand(user.Id, code2), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.RequestID.Should().NotBeNull();

        var saved = await _db.FriendRequests.FirstOrDefaultAsync( r=> r.Id == result.RequestID);
        saved.Should().NotBeNull();
        saved!.SenderId.Should().Be(user.Id);
        saved.ReceiverId.Should().Be(user2.Id);
        saved.Status.Should().Be(FriendshipHelpers.statusPending);
    }

    //RespondToFriendRequestHandler tests - all passing
    [Fact]
    public async Task RespondToRequest_SHouldFail_WhenUserNotFound()
    {
        var user = await SeedUserAsync("Dany", code1);
        var handler = new RespondToFriendRequestHandler(_db);
        var result = await handler.Handle(new RespondToFriendRequestCommand(user.Id, Guid.NewGuid(), Accept: true), CancellationToken.None);

        result.Should().BeFalse();
    }
    [Fact]
    public async Task RespondToRequest_ShouldAcceptAndCreateFriendship_WhenAccepted()
    {
        var user = await SeedUserAsync("Dany", code1);
        var user2 = await SeedUserAsync("Viserys", code2);
        var req = new FriendRequest
        {
            SenderId = user.Id,
            ReceiverId = user2.Id,
            Status = FriendshipHelpers.statusPending,
            CreatedAt = DateTime.UtcNow
        };
        _db.FriendRequests.Add(req);
        await _db.SaveChangesAsync();

        var handler = new RespondToFriendRequestHandler(_db);
        var result = await handler.Handle(new RespondToFriendRequestCommand(user2.Id, req.Id, Accept: true), CancellationToken.None);

        result.Should().BeTrue();

        var updatedreq = await _db.FriendRequests.FindAsync(req.Id);
        updatedreq!.Status.Should().Be(FriendshipHelpers.statusAccepted);
        updatedreq.RespondedAt.Should().NotBeNull();
        var (u1, u2) = FriendshipHelpers.toCanonOrder(user.Id, user2.Id);
        var friends = await _db.Friendships.FirstOrDefaultAsync(f => f.UserId1 == u1 && f.UserId2 == u2);
        friends.Should().NotBeNull();
    }

    [Fact]
    public async Task RespondToRequest_ShouldRejectandNoFriendship_WhenRejected()
    {
        var user = await SeedUserAsync("Dany", code1);
        var user2 = await SeedUserAsync("Cersei", code2);
        var req = new FriendRequest
        {
            SenderId = user.Id,
            ReceiverId = user2.Id,
            Status = FriendshipHelpers.statusPending,
            CreatedAt = DateTime.UtcNow
        };
        _db.FriendRequests.Add(req);
        await _db.SaveChangesAsync();

        var handler = new RespondToFriendRequestHandler(_db);
        var result = await handler.Handle(new RespondToFriendRequestCommand(user2.Id, req.Id, Accept: false), CancellationToken.None);

        result.Should().BeTrue();

        var updatedreq = await _db.FriendRequests.FindAsync(req.Id);
        updatedreq!.Status.Should().Be(FriendshipHelpers.statusRejected);
        updatedreq.RespondedAt.Should().NotBeNull();
        var friends = await _db.Friendships.CountAsync();
        friends.Should().Be(0);
    }

    //RejectAllFriendRequests handler tests - all passing
    [Fact]
    public async Task RejectAll_ShouldReturnZero_WhenNoPending()
    {
        var user = await SeedUserAsync("Dany", code1);
        var handler = new RejectAllFriendRequestsHandler(_db);
        var result = await handler.Handle(new RejectAllFriendRequestsCommand(user.Id), CancellationToken.None);

        result.Should().Be(0);
    }
    [Fact]
    public async Task RejectAll_ShouldRejectPending_AndReturnCorrectCount()
    {
        var user = await SeedUserAsync("Dany", code1);
        var user2 = await SeedUserAsync("Cersei", code2);
        var user3 = await SeedUserAsync("Dovagedis", code3);
        var req = new FriendRequest
        {
            SenderId = user.Id,
            ReceiverId = user2.Id,
            Status = FriendshipHelpers.statusPending,
            CreatedAt = DateTime.UtcNow
        };
        _db.FriendRequests.Add(req);
        var req2 = new FriendRequest
        {
            SenderId = user3.Id,
            ReceiverId = user2.Id,
            Status = FriendshipHelpers.statusPending,
            CreatedAt = DateTime.UtcNow
        };
        _db.FriendRequests.Add(req2);
        await _db.SaveChangesAsync();

        var handler = new RejectAllFriendRequestsHandler(_db);
        var result = await handler.Handle(new RejectAllFriendRequestsCommand(user2.Id), CancellationToken.None);

        result.Should().Be(2);

        var remaining = await _db.FriendRequests.CountAsync(r => r.ReceiverId == user2.Id && r.Status == FriendshipHelpers.statusPending);
        remaining.Should().Be(0);
    }

    //RemoveFriendHandler tests - all passing
    [Fact]
    public async Task RemoveFriend_ShouldReturnFalse_WhenFriendshipNotFound()
    {
        var user = await SeedUserAsync("Dany", code1);
        var user2 = await SeedUserAsync("Jaime", code2);
        var handler = new RemoveFriendHandler(_db);
        var result = await handler.Handle(new RemoveFriendCommand(user.Id, user2.Id), CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveFriend_ShouldRemoveFriendship_WhenExists()
    {
        var user = await SeedUserAsync("Dany", code1);
        var user2 = await SeedUserAsync("Drogo", code2);
        var (u1, u2) = FriendshipHelpers.toCanonOrder(user.Id, user2.Id);
        _db.Friendships.Add(new Friendship
        {
            UserId1 = u1,
            UserId2= u2,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var handler = new RemoveFriendHandler(_db);
        var result = await handler.Handle(new RemoveFriendCommand(user.Id, user2.Id), CancellationToken.None);

        result.Should().BeTrue();
        var remaining = await _db.Friendships.CountAsync();
        remaining.Should().Be(0);
    }

    //GetFriendsListHandler tests - all passing
    [Fact]
    public async Task GetFriendsList_ShouldReturnEmpty_WhenNoFriends()
    {
        var user = await SeedUserAsync("Dany", code1);
        var handler = new GetFriendsListHandler(_db);
        var result = await handler.Handle(new GetFriendsListQuery(user.Id), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFriendsList_ShouldReturnFriends_WithInitsAndFriendCode()
    {
        var user = await SeedUserAsync("Dany", code1);
        var user2 = await SeedUserAsync("Khal Drogo", code2);
        var (u1, u2) = FriendshipHelpers.toCanonOrder(user.Id, user2.Id);
        _db.Friendships.Add(new Friendship
        {
            UserId1 = u1,
            UserId2= u2,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var handler = new GetFriendsListHandler(_db);
        var result = await handler.Handle(new GetFriendsListQuery(user.Id), CancellationToken.None);

        result.Should().HaveCount(1);
        var friend = result[0];
        friend.Id.Should().Be(user2.Id);
        friend.Name.Should().Be("Khal Drogo");
        friend.Initials.Should().Be("KD");
        friend.Code.Should().Be(code2);
        friend.Tier.Should().Be("Bronze");
    }

    //GetPendingFriendRequestsHandler tests - all pass
    [Fact]
    public async Task GetPendingRequests_ShouldReturnIncoming_AndOutgoing()
    {
        var user = await SeedUserAsync("Dany", code1);
        var user2 = await SeedUserAsync("Cersei", code2);
        var user3 = await SeedUserAsync("Dovagedis", code3);
        var req = new FriendRequest
        {
            SenderId = user.Id,
            ReceiverId = user2.Id,
            Status = FriendshipHelpers.statusPending,
            CreatedAt = DateTime.UtcNow
        };
        _db.FriendRequests.Add(req);
        var req2 = new FriendRequest
        {
            SenderId = user2.Id,
            ReceiverId = user3.Id,
            Status = FriendshipHelpers.statusPending,
            CreatedAt = DateTime.UtcNow
        };
        _db.FriendRequests.Add(req2);
        await _db.SaveChangesAsync();

        var handler = new GetPendingFriendRequestsHandler(_db);
        var result = await handler.Handle(new GetPendingFriendRequestsQuery(user2.Id), CancellationToken.None);

        result.Incoming.Should().HaveCount(1);
        result.Incoming[0].FromName.Should().Be("Dany");
        result.Incoming[0].FromCode.Should().Be(code1);
        result.Outgoing.Should().HaveCount(1);
        result.Outgoing[0].FromName.Should().Be("Dovagedis");
        result.Outgoing[0].FromCode.Should().Be(code3);
    }
    //GetMyFriendCodeHandler tests - all passing
    [Fact]
    public async Task GetMyFriendCode_ShouldReturnCode_WhenUserExists()
    {
        var user = await SeedUserAsync("Dany", code1);
        var handler = new GetMyFriendCodeHandler(_db);
        var result = await handler.Handle(new GetMyFriendCodeQuery(user.Id), CancellationToken.None);

        result.Should().Be(code1);
    }
    [Fact]
    public async Task GetMyFriendCode_ShouldReturnNull_WhenUserDoesNotExists()
    {
        var handler = new GetMyFriendCodeHandler(_db);
        var result = await handler.Handle(new GetMyFriendCodeQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

}