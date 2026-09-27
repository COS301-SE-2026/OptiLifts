using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Friends.Commands;
using OptiLifts.Tests.Integration.IntegrationDb;

namespace OptiLifts.Tests.Integration;

[Collection("SharedDatabase")]
public sealed class FriendsEndpointIntegrationTests : IntegrationTestBase
{
    public FriendsEndpointIntegrationTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    private void AuthAs(Guid userId)
    {
        Client.DefaultRequestHeaders.Remove("Cookie");
        Client.DefaultRequestHeaders.Add("Cookie", $"access_token={GenerateToken(userId)}");
    }

    private async Task<string> GetMyCodeAsync()
    {
        var resp = await Client.GetAsync("/api/clash/friends/code");
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("code").GetString()!;
    }

    [Fact]
    public async Task GetFriends_Unauthenticated_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Remove("Cookie");

        var resp = await Client.GetAsync("/api/clash/friends");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyCode_ReturnsSixCharCode()
    {
        var userId = await SeedUserAsync("friends-code@optilifts.com");
        AuthAs(userId);

        var code = await GetMyCodeAsync();

        code.Should().NotBeNullOrWhiteSpace();
        code.Length.Should().Be(6);
    }

    [Fact]
    public async Task SendRequest_CreatesPending_VisibleBothSides()
    {
        var senderId = await SeedUserAsync("friend-sender@optilifts.com");
        var receiverId = await SeedUserAsync("friend-receiver@optilifts.com");

        AuthAs(receiverId);
        var receiverCode = await GetMyCodeAsync();

        AuthAs(senderId);
        var sendResp = await Client.PostAsJsonAsync("/api/clash/friends/requests", new FriendsController.SendFriendRequestApiRequest(receiverCode));
        sendResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var sendRes = await sendResp.Content.ReadFromJsonAsync<SendFriendRequestResult>();
        sendRes!.Success.Should().BeTrue();

        var outgoing = await Client.GetFromJsonAsync<PendingFriendRequestsResult>("/api/clash/friends/requests");
        outgoing!.Outgoing.Should().ContainSingle(r => r.FromCode == receiverCode);

        AuthAs(receiverId);
        var incoming = await Client.GetFromJsonAsync<PendingFriendRequestsResult>("/api/clash/friends/requests");
        incoming!.Incoming.Should().HaveCount(1);
    }

    [Fact]
    public async Task RespondAccept_CreatesMutualFriendship()
    {
        var senderId = await SeedUserAsync("friend-accept-sender@optilifts.com");
        var receiverId = await SeedUserAsync("friend-accept-receiver@optilifts.com");

        AuthAs(receiverId);
        var receiverCode = await GetMyCodeAsync();

        AuthAs(senderId);
        await Client.PostAsJsonAsync("/api/clash/friends/requests", new FriendsController.SendFriendRequestApiRequest(receiverCode));

        AuthAs(receiverId);
        var pending = await Client.GetFromJsonAsync<PendingFriendRequestsResult>("/api/clash/friends/requests");
        var reqId = pending!.Incoming.Single().Id;

        var respondResp = await Client.PostAsJsonAsync($"/api/clash/friends/requests/{reqId}/respond", new FriendsController.RespondFriendRequestApiRequest(true));
        respondResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var receiverFriends = await Client.GetFromJsonAsync<List<FriendDto>>("/api/clash/friends");
        receiverFriends.Should().ContainSingle(f => f.Id == senderId);

        AuthAs(senderId);
        var senderFriends = await Client.GetFromJsonAsync<List<FriendDto>>("/api/clash/friends");
        senderFriends.Should().ContainSingle(f => f.Id == receiverId);
    }

    [Fact]
    public async Task RespondDecline_NoFriendshipCreated()
    {
        var senderId = await SeedUserAsync("friend-decline-sender@optilifts.com");
        var receiverId = await SeedUserAsync("friend-decline-receiver@optilifts.com");

        AuthAs(receiverId);
        var receiverCode = await GetMyCodeAsync();

        AuthAs(senderId);
        await Client.PostAsJsonAsync("/api/clash/friends/requests", new FriendsController.SendFriendRequestApiRequest(receiverCode));

        AuthAs(receiverId);
        var pending = await Client.GetFromJsonAsync<PendingFriendRequestsResult>("/api/clash/friends/requests");
        var reqId = pending!.Incoming.Single().Id;

        var respondResp = await Client.PostAsJsonAsync($"/api/clash/friends/requests/{reqId}/respond", new FriendsController.RespondFriendRequestApiRequest(false));
        respondResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var receiverFriends = await Client.GetFromJsonAsync<List<FriendDto>>("/api/clash/friends");
        receiverFriends.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveFriend_DeletesFriendship()
    {
        var userAId = await SeedUserAsync("friend-remove-a@optilifts.com");
        var userBId = await SeedUserAsync("friend-remove-b@optilifts.com");

        AuthAs(userBId);
        var codeB = await GetMyCodeAsync();

        AuthAs(userAId);
        await Client.PostAsJsonAsync("/api/clash/friends/requests", new FriendsController.SendFriendRequestApiRequest(codeB));

        AuthAs(userBId);
        var pending = await Client.GetFromJsonAsync<PendingFriendRequestsResult>("/api/clash/friends/requests");
        var reqId = pending!.Incoming.Single().Id;
        await Client.PostAsJsonAsync($"/api/clash/friends/requests/{reqId}/respond", new FriendsController.RespondFriendRequestApiRequest(true));

        var delResp = await Client.DeleteAsync($"/api/clash/friends/{userAId}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var friendsAfter = await Client.GetFromJsonAsync<List<FriendDto>>("/api/clash/friends");
        friendsAfter.Should().BeEmpty();
    }

    [Fact]
    public async Task RejectAll_RejectsAllPendingIncoming()
    {
        var receiverId = await SeedUserAsync("friend-rejectall-receiver@optilifts.com");
        var senderAId = await SeedUserAsync("friend-rejectall-a@optilifts.com");
        var senderBId = await SeedUserAsync("friend-rejectall-b@optilifts.com");

        AuthAs(receiverId);
        var receiverCode = await GetMyCodeAsync();

        AuthAs(senderAId);
        await Client.PostAsJsonAsync("/api/clash/friends/requests", new FriendsController.SendFriendRequestApiRequest(receiverCode));

        AuthAs(senderBId);
        await Client.PostAsJsonAsync("/api/clash/friends/requests", new FriendsController.SendFriendRequestApiRequest(receiverCode));

        AuthAs(receiverId);
        var rejectResp = await Client.PostAsync("/api/clash/friends/requests/reject-all", null);
        rejectResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rejectJson = await rejectResp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(rejectJson);
        doc.RootElement.GetProperty("rejectCount").GetInt32().Should().Be(2);

        var pendingAfter = await Client.GetFromJsonAsync<PendingFriendRequestsResult>("/api/clash/friends/requests");
        pendingAfter!.Incoming.Should().BeEmpty();
    }
}
