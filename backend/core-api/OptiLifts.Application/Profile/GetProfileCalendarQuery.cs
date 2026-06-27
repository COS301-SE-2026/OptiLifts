using MediatR;

namespace OptiLifts.Application.Profile;

public sealed record GetProfileCalendarQuery(Guid UserId, int Year, int Month) : IRequest<ProfileCalendarDto>;