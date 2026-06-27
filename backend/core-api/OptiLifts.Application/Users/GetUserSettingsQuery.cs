using System;
using MediatR;

namespace OptiLifts.Application.Users;

public sealed record GetUserSettingsQuery(Guid UserId) : IRequest<UserSettingsDto>;

