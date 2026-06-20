using MediatR;

namespace OptiLifts.Application.Profile;

public sealed record GetProfileOverviewQuery(Guid UserId) : IRequest<ProfileOverviewDto>;