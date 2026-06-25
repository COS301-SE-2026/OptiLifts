using OptiLifts.Domain.Gamification;

namespace OptiLifts.Application.Gamification.Abstraction;

public interface IBadgeRule
{
    string Code { get; }
    Task<bool> IsEarnedAsync(Guid userId, Badge bage, CancellationToken cancellationToken);
}