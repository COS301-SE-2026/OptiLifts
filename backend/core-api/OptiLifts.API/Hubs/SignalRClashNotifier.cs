using Microsoft.AspNetCore.SignalR;
using OptiLifts.Application.Clash;
using OptiLifts.Application.Clash.Arenas;

namespace OptiLifts.API.Hubs;

public sealed class SignalRClashNotifier : IClashNotifier
{
    private readonly IHubContext<ClashHub, IClashClient> _hubContext;

    public SignalRClashNotifier(IHubContext<ClashHub, IClashClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastActivityAsync(string arenaId, ClashActivityDto activity, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Arena_{arenaId}").ReceiveActivity(activity);
    }

    public async Task BroadcastKudosAsync(string arenaId, Guid activityId, int kudosCount, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Arena_{arenaId}").ReceiveKudos(activityId, kudosCount);
    }

    public async Task BroadcastUserJoinedAsync(string arenaId, string userName, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Arena_{arenaId}").ReceiveUserJoined(arenaId, userName);
    }

    public async Task BroadcastUserLeftAsync(string arenaId, string userName, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"Arena_{arenaId}").ReceiveUserLeft(arenaId, userName);
    }
}
