using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Duels;

namespace OptiLifts.API.Hubs;

public interface IClashClient
{
    Task ReceiveDuelUpdate(DuelUpdateDto update);
    Task ReceiveDuelHype(Guid duelId, string senderName);
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

    public async Task JoinDuel(string duelId)
    {
        if (!string.IsNullOrWhiteSpace(duelId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Duel_{duelId}");
        }
    }

    public async Task LeaveDuel(string duelId)
    {
        if (!string.IsNullOrWhiteSpace(duelId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Duel_{duelId}");
        }
    }
}
