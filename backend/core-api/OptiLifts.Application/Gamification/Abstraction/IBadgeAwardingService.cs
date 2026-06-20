using OptiLifts.Domain.Gamification;

namespace OptiLifts.Application.Gamification.Abstraction;

public interface IBadgeAwardingService
{
    Task AwardEligibleAsync(Guid userId, CancellationToken cancellationToken);
}