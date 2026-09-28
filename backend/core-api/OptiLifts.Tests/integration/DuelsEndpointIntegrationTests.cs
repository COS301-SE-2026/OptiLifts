using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Clash.Duels.Commands;
using OptiLifts.Application.Clash.Duels.Queries;
using OptiLifts.Domain.Clash;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class DuelsEndpointIntegrationTests : IntegrationTestBase
{
    public DuelsEndpointIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private void AuthAs(Guid userId)
    {
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");
    }

    private async Task SeedFriendshipAsync(Guid userAId, Guid userBId)
    {
        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OptiLiftsDbContext>();
        var (u1, u2) = userAId.CompareTo(userBId) < 0 ? (userAId, userBId) : (userBId, userAId);
        db.Friendships.Add(new Friendship { UserId1 = u1, UserId2 = u2 });
        await db.SaveChangesAsync();
    }

    private static DuelsController.CreateDuelApiRequest ChallengeRequest(Guid rivalId) =>
        new(rivalId, "Barbell Back Squat", null, "E1RMGainPercent", 7);

    [Fact]
    public async Task GetDuels_Unauthenticated_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Remove("Cookie");

        var resp = await Client.GetAsync("/api/clash/duels");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateDuel_NotFriends_ReturnsBadRequest()
    {
        var challengerId = await SeedUserAsync("duel-notfriends-challenger@optilifts.com");
        var rivalId = await SeedUserAsync("duel-notfriends-rival@optilifts.com");

        AuthAs(challengerId);
        var resp = await Client.PostAsJsonAsync("/api/clash/duels", ChallengeRequest(rivalId));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var res = await resp.Content.ReadFromJsonAsync<CreateDuelChallengeResult>();
        res!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CreateDuel_CreatesPending_VisibleAsInviteToRival()
    {
        var challengerId = await SeedUserAsync("duel-create-challenger@optilifts.com");
        var rivalId = await SeedUserAsync("duel-create-rival@optilifts.com");
        await SeedFriendshipAsync(challengerId, rivalId);

        AuthAs(challengerId);
        var resp = await Client.PostAsJsonAsync("/api/clash/duels", ChallengeRequest(rivalId));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var res = await resp.Content.ReadFromJsonAsync<CreateDuelChallengeResult>();
        res!.Success.Should().BeTrue();

        AuthAs(rivalId);
        var invites = await Client.GetFromJsonAsync<List<DuelInviteDto>>("/api/clash/duels/invites");
        invites.Should().ContainSingle(i => i.ChallengerUserId == challengerId);
    }

    [Fact]
    public async Task RespondAccept_ActivatesDuel_AppearsInBothDuelLists()
    {
        var challengerId = await SeedUserAsync("duel-accept-challenger@optilifts.com");
        var rivalId = await SeedUserAsync("duel-accept-rival@optilifts.com");
        await SeedFriendshipAsync(challengerId, rivalId);

        AuthAs(challengerId);
        await Client.PostAsJsonAsync("/api/clash/duels", ChallengeRequest(rivalId));

        AuthAs(rivalId);
        var invites = await Client.GetFromJsonAsync<List<DuelInviteDto>>("/api/clash/duels/invites");
        var duelId = invites!.Single().Id;

        var respondResp = await Client.PostAsJsonAsync($"/api/clash/duels/{duelId}/respond", new DuelsController.RespondDuelApiRequest(true));
        respondResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var rivalDuels = await Client.GetFromJsonAsync<UserDuelsResult>("/api/clash/duels");
        rivalDuels!.Active.Should().Be(1);
        rivalDuels.Duels.Should().ContainSingle(d => d.Id == duelId && d.Status == "Active");

        AuthAs(challengerId);
        var challengerDuels = await Client.GetFromJsonAsync<UserDuelsResult>("/api/clash/duels");
        challengerDuels!.Duels.Should().ContainSingle(d => d.Id == duelId && d.Status == "Active");
    }

    [Fact]
    public async Task RespondDecline_RemovesFromInvites_NoActiveDuel()
    {
        var challengerId = await SeedUserAsync("duel-decline-challenger@optilifts.com");
        var rivalId = await SeedUserAsync("duel-decline-rival@optilifts.com");
        await SeedFriendshipAsync(challengerId, rivalId);

        AuthAs(challengerId);
        await Client.PostAsJsonAsync("/api/clash/duels", ChallengeRequest(rivalId));

        AuthAs(rivalId);
        var invites = await Client.GetFromJsonAsync<List<DuelInviteDto>>("/api/clash/duels/invites");
        var duelId = invites!.Single().Id;

        var respondResp = await Client.PostAsJsonAsync($"/api/clash/duels/{duelId}/respond", new DuelsController.RespondDuelApiRequest(false));
        respondResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var invitesAfter = await Client.GetFromJsonAsync<List<DuelInviteDto>>("/api/clash/duels/invites");
        invitesAfter.Should().BeEmpty();

        var duelsAfter = await Client.GetFromJsonAsync<UserDuelsResult>("/api/clash/duels");
        duelsAfter!.Active.Should().Be(0);
    }

    [Fact]
    public async Task SendHype_Succeeds_ForActiveDuelParticipant()
    {
        var challengerId = await SeedUserAsync("duel-hype-challenger@optilifts.com");
        var rivalId = await SeedUserAsync("duel-hype-rival@optilifts.com");
        await SeedFriendshipAsync(challengerId, rivalId);

        AuthAs(challengerId);
        await Client.PostAsJsonAsync("/api/clash/duels", ChallengeRequest(rivalId));

        AuthAs(rivalId);
        var invites = await Client.GetFromJsonAsync<List<DuelInviteDto>>("/api/clash/duels/invites");
        var duelId = invites!.Single().Id;
        await Client.PostAsJsonAsync($"/api/clash/duels/{duelId}/respond", new DuelsController.RespondDuelApiRequest(true));

        var hypeResp = await Client.PostAsync($"/api/clash/duels/{duelId}/hype", null);
        hypeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var hypeRes = await hypeResp.Content.ReadFromJsonAsync<SendDuelHypeResult>();
        hypeRes!.Success.Should().BeTrue();
    }
}
