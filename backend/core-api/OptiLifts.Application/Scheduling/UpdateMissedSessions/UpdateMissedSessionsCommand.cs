using MediatR;
namespace OptiLifts.Application.Scheduling.UpdateMissedSessions;

public sealed record UpdateMissedSessionsCommand(Guid UserId) : IRequest<UpdateMissedSessionsResult>;
public sealed record UpdateMissedSessionsResult(int UpdatedCount);