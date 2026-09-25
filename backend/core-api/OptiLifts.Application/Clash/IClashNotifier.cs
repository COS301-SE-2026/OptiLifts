namespace OptiLifts.Application.Clash;

using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Duels;

public interface IClashNotifier
{
    Task BroadcastDuelUpdateAsync(Guid duelId, DuelUpdateDto update, CancellationToken cancellationToken = default);
    Task BroadcastDuelHypeAsync(Guid duelId, string senderName, CancellationToken cancellationToken = default);
    Task BroadcastActivityAsync(string arenaId, ClashActivityDto activity, CancellationToken cancellationToken = default);
    Task BroadcastKudosAsync(string arenaId, Guid activityId, int kudosCount, CancellationToken cancellationToken = default);
    Task BroadcastUserJoinedAsync(string arenaId, string userName, CancellationToken cancellationToken = default);
    Task BroadcastUserLeftAsync(string arenaId, string userName, CancellationToken cancellationToken = default);
}

public sealed class NullClashNotifier : IClashNotifier
{
    public static readonly NullClashNotifier Instance = new();

    public Task BroadcastDuelUpdateAsync(Guid duelId, DuelUpdateDto update, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task BroadcastDuelHypeAsync(Guid duelId, string senderName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task BroadcastActivityAsync(string arenaId, ClashActivityDto activity, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task BroadcastKudosAsync(string arenaId, Guid activityId, int kudosCount, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task BroadcastUserJoinedAsync(string arenaId, string userName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task BroadcastUserLeftAsync(string arenaId, string userName, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
