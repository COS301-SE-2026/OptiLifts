using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OptiLifts.Application.Clash.Arenas;

namespace OptiLifts.API.Hubs;

public interface IClashClient
{
    Task ReceiveActivity(ClashActivityDto activity);
    Task ReceiveKudos(Guid activityId, int kudosCount);
    Task ReceiveUserJoined(string arenaId, string userName);
    Task ReceiveUserLeft(string arenaId, string userName);
}

[Authorize]
public class ClashHub : Hub<IClashClient>
{
    public async Task JoinArena(string arenaId)
    {
        if (!string.IsNullOrWhiteSpace(arenaId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Arena_{arenaId}");
        }
    }

    public async Task LeaveArena(string arenaId)
    {
        if (!string.IsNullOrWhiteSpace(arenaId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Arena_{arenaId}");
        }
    }
}
