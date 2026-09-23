using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;
using Xunit;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class ArenasEndpointIntegrationTests : IntegrationTestBase
{
    public ArenasEndpointIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private void AuthenticateClient(Guid userId)
    {
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");
    }

    private async Task EstablishFriendshipAsync(Guid u1, Guid u2)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var (c1, c2) = FriendshipHelpers.toCanonOrder(u1, u2);
        db.Friendships.Add(new Friendship
        {
            UserId1 = c1,
            UserId2 = c2,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Unauthenticated_Requests_ShouldReturnUnauthorized()
    {
        Client.DefaultRequestHeaders.Remove("Cookie");

        var createRes = await Client.PostAsJsonAsync("/api/v1/clash/arenas", new ArenasController.CreateArenaApiRequest("Squad", "DotsOverall", 30));
        createRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var myRes = await Client.GetAsync("/api/v1/clash/arenas/my");
        myRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var joinRes = await Client.PostAsJsonAsync("/api/v1/clash/arenas/join", new ArenasController.JoinArenaApiRequest("CODE12"));
        joinRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateArena_AndGetMyArenas_ShouldSucceed()
    {
        var userId = await SeedUserAsync("creator@opticlash.com");
        AuthenticateClient(userId);

        var request = new ArenasController.CreateArenaApiRequest("Titan Squad", "DotsOverall", 30);
        var createResponse = await Client.PostAsJsonAsync("/api/v1/clash/arenas", request);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateArenaResult>();

        createResult.Should().NotBeNull();
        createResult!.Success.Should().BeTrue();
        createResult.Arena.Should().NotBeNull();
        createResult.Arena!.Name.Should().Be("Titan Squad");
        createResult.Arena.Code.Should().HaveLength(6);
        createResult.Arena.UserRole.Should().Be("Owner");
        createResult.Arena.MemberCount.Should().Be(1);

        var myResponse = await Client.GetAsync("/api/v1/clash/arenas/my");
        myResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var myArenas = await myResponse.Content.ReadFromJsonAsync<List<ArenaDto>>();

        myArenas.Should().NotBeNull();
        myArenas.Should().ContainSingle(a => a.Name == "Titan Squad" && a.UserRole == "Owner");
    }

    [Fact]
    public async Task JoinArenaByCode_ShouldSucceed_AndReflectOnLeaderboard()
    {
        var ownerId = await SeedUserAsync("owner@opticlash.com");
        var joinerId = await SeedUserAsync("joiner@opticlash.com");

        AuthenticateClient(ownerId);
        var createRes = await Client.PostAsJsonAsync("/api/v1/clash/arenas", new ArenasController.CreateArenaApiRequest("Alpha Squad", "DotsOverall", 30));
        var createResult = await createRes.Content.ReadFromJsonAsync<CreateArenaResult>();
        var arenaCode = createResult!.Arena!.Code!;
        var arenaId = createResult.Arena.Id;

        AuthenticateClient(joinerId);
        var joinRes = await Client.PostAsJsonAsync("/api/v1/clash/arenas/join", new ArenasController.JoinArenaApiRequest(arenaCode));
        joinRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var joinResult = await joinRes.Content.ReadFromJsonAsync<JoinArenaResult>();

        joinResult.Should().NotBeNull();
        joinResult!.Success.Should().BeTrue();
        joinResult.Arena!.MemberCount.Should().Be(2);

        var leaderboardRes = await Client.GetAsync($"/api/v1/clash/arenas/{arenaId}");
        leaderboardRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var leaderboard = await leaderboardRes.Content.ReadFromJsonAsync<ArenaLeaderboardResult>();

        leaderboard.Should().NotBeNull();
        leaderboard!.Standings.Should().HaveCount(2);
        leaderboard.UserStanding.Should().NotBeNull();
        leaderboard.UserStanding!.UserId.Should().Be(joinerId);
    }

    [Fact]
    public async Task InviteFriend_AndRespond_ShouldAddMember()
    {
        var ownerId = await SeedUserAsync("inviter@opticlash.com");
        var friendId = await SeedUserAsync("invited@opticlash.com");
        await EstablishFriendshipAsync(ownerId, friendId);

        AuthenticateClient(ownerId);
        var createRes = await Client.PostAsJsonAsync("/api/v1/clash/arenas", new ArenasController.CreateArenaApiRequest("Friend Arena", "TotalVolume", 14));
        var createResult = await createRes.Content.ReadFromJsonAsync<CreateArenaResult>();
        var arenaId = createResult!.Arena!.Id;

        var inviteRes = await Client.PostAsJsonAsync($"/api/v1/clash/arenas/{arenaId}/invite", new ArenasController.InviteFriendApiRequest(friendId));
        inviteRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var inviteResult = await inviteRes.Content.ReadFromJsonAsync<InviteFriendToArenaResult>();
        inviteResult!.Success.Should().BeTrue();

        AuthenticateClient(friendId);
        var getInvitesRes = await Client.GetAsync("/api/v1/clash/arenas/invites");
        getInvitesRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var invites = await getInvitesRes.Content.ReadFromJsonAsync<List<ArenaInviteDto>>();

        invites.Should().NotBeNull();
        invites.Should().ContainSingle(i => i.ArenaId == arenaId && i.Status == "Pending");

        var inviteId = invites!.First(i => i.ArenaId == arenaId).Id;

        var respondRes = await Client.PostAsJsonAsync($"/api/v1/clash/arenas/invites/{inviteId}/respond", new ArenasController.RespondArenaInviteApiRequest(true));
        respondRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var myRes = await Client.GetAsync("/api/v1/clash/arenas/my");
        var myArenas = await myRes.Content.ReadFromJsonAsync<List<ArenaDto>>();
        myArenas.Should().ContainSingle(a => a.Id == arenaId && a.UserRole == "Member");
    }

    [Fact]
    public async Task SendActivityKudos_AndGetFeed_ShouldReflectKudos()
    {
        var ownerId = await SeedUserAsync("kudos_owner@opticlash.com");
        var fanId = await SeedUserAsync("kudos_fan@opticlash.com");

        AuthenticateClient(ownerId);
        var createRes = await Client.PostAsJsonAsync("/api/v1/clash/arenas", new ArenasController.CreateArenaApiRequest("Hype Arena", "DotsOverall", 30));
        var createResult = await createRes.Content.ReadFromJsonAsync<CreateArenaResult>();
        var arenaId = createResult!.Arena!.Id;

        var feedRes = await Client.GetAsync($"/api/v1/clash/arenas/{arenaId}/feed");
        feedRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var feed = await feedRes.Content.ReadFromJsonAsync<List<ClashActivityDto>>();
        feed.Should().NotBeEmpty();

        var activityId = feed!.First().Id;

        AuthenticateClient(fanId);
        var kudosRes = await Client.PostAsync($"/api/v1/clash/activities/{activityId}/kudos", null);
        kudosRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var kudosResult = await kudosRes.Content.ReadFromJsonAsync<SendActivityKudosResult>();
        kudosResult!.Success.Should().BeTrue();
        kudosResult.KudosCount.Should().Be(1);

        var dupKudosRes = await Client.PostAsync($"/api/v1/clash/activities/{activityId}/kudos", null);
        dupKudosRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var updatedFeedRes = await Client.GetAsync($"/api/v1/clash/arenas/{arenaId}/feed");
        var updatedFeed = await updatedFeedRes.Content.ReadFromJsonAsync<List<ClashActivityDto>>();
        updatedFeed!.First(a => a.Id == activityId).HasUserKudoed.Should().BeTrue();
    }

    [Fact]
    public async Task LeaveArena_ShouldSucceedForMember_AndFailForOwner()
    {
        var ownerId = await SeedUserAsync("leave_owner@opticlash.com");
        var memberId = await SeedUserAsync("leave_member@opticlash.com");

        AuthenticateClient(ownerId);
        var createRes = await Client.PostAsJsonAsync("/api/v1/clash/arenas", new ArenasController.CreateArenaApiRequest("Departure Squad", "DotsOverall", 30));
        var createResult = await createRes.Content.ReadFromJsonAsync<CreateArenaResult>();
        var arenaId = createResult!.Arena!.Id;
        var arenaCode = createResult.Arena.Code!;

        AuthenticateClient(memberId);
        await Client.PostAsJsonAsync("/api/v1/clash/arenas/join", new ArenasController.JoinArenaApiRequest(arenaCode));

        var leaveMemberRes = await Client.PostAsync($"/api/v1/clash/arenas/{arenaId}/leave", null);
        leaveMemberRes.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthenticateClient(ownerId);
        var leaveOwnerRes = await Client.PostAsync($"/api/v1/clash/arenas/{arenaId}/leave", null);
        leaveOwnerRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
