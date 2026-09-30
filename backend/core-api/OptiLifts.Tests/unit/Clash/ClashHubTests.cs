using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using OptiLifts.API.Hubs;
using OptiLifts.Application.Clash.Arenas;
using Xunit;

namespace OptiLifts.Tests.Unit.Clash;

public sealed class ClashHubTests
{
    [Fact]
    public async Task JoinArena_ShouldAddToGroup()
    {
        var groupManagerMock = new Mock<IGroupManager>();
        var hubCallerContextMock = new Mock<HubCallerContext>();
        hubCallerContextMock.Setup(c => c.ConnectionId).Returns("conn-123");

        var hub = new ClashHub
        {
            Context = hubCallerContextMock.Object,
            Groups = groupManagerMock.Object
        };

        await hub.JoinArena("arena-xyz");

        groupManagerMock.Verify(g => g.AddToGroupAsync("conn-123", "Arena_arena-xyz", default), Times.Once);
    }

    [Fact]
    public async Task LeaveArena_ShouldRemoveFromGroup()
    {
        var groupManagerMock = new Mock<IGroupManager>();
        var hubCallerContextMock = new Mock<HubCallerContext>();
        hubCallerContextMock.Setup(c => c.ConnectionId).Returns("conn-123");

        var hub = new ClashHub
        {
            Context = hubCallerContextMock.Object,
            Groups = groupManagerMock.Object
        };

        await hub.LeaveArena("arena-xyz");

        groupManagerMock.Verify(g => g.RemoveFromGroupAsync("conn-123", "Arena_arena-xyz", default), Times.Once);
    }

    [Fact]
    public async Task SignalRClashNotifier_ShouldBroadcastActivity()
    {
        var clientMock = new Mock<IClashClient>();
        var clientsMock = new Mock<IHubClients<IClashClient>>();
        clientsMock.Setup(c => c.Group("Arena_arena-1")).Returns(clientMock.Object);

        var hubContextMock = new Mock<IHubContext<ClashHub, IClashClient>>();
        hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var notifier = new SignalRClashNotifier(hubContextMock.Object);
        var activityDto = new ClashActivityDto(
            Id: Guid.NewGuid(),
            ArenaId: "arena-1",
            UserId: Guid.NewGuid(),
            UserName: "Athlete",
            UserInitials: "AT",
            UserAvatarUrl: null,
            EventText: "PR achieved!",
            Details: "100kg Bench",
            IsPr: true,
            IsPromotion: false,
            KudosCount: 0,
            HasUserKudoed: false,
            CreatedAt: DateTime.UtcNow
        );

        await notifier.BroadcastActivityAsync("arena-1", activityDto);

        clientMock.Verify(c => c.ReceiveActivity(activityDto), Times.Once);
    }

    [Fact]
    public async Task SignalRClashNotifier_ShouldBroadcastKudos()
    {
        var clientMock = new Mock<IClashClient>();
        var clientsMock = new Mock<IHubClients<IClashClient>>();
        clientsMock.Setup(c => c.Group("Arena_arena-1")).Returns(clientMock.Object);

        var hubContextMock = new Mock<IHubContext<ClashHub, IClashClient>>();
        hubContextMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        var notifier = new SignalRClashNotifier(hubContextMock.Object);
        var activityId = Guid.NewGuid();

        await notifier.BroadcastKudosAsync("arena-1", activityId, 5);

        clientMock.Verify(c => c.ReceiveKudos(activityId, 5), Times.Once);
    }
}
