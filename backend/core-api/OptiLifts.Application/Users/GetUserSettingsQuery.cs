using MediatR;
using System;

namespace OptiLifts.Application.Users;

public sealed record GetUserSettingsQuery(Guid UserId) : IRequest<UserSettingsDto>;

