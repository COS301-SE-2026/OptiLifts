using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.Reschedule;
using OptiLifts.Application.Users;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Scheduling.Reschedule;

public class GetUserScheduleConfigHandler : IRequestHandler<GetUserScheduleConfigQuery, UserScheduleConfigDto>
{
    private readonly OptiLiftsDbContext _dbContext;
    public GetUserScheduleConfigHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserScheduleConfigDto> Handle(GetUserScheduleConfigQuery request, CancellationToken cancellationToken)
    {
        var config = await _dbContext.UserScheduleConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.UserId ==request.UserId, cancellationToken);
        if (config == null)
        {
            config = new UserScheduleConfig
            {
                UserId = request.UserId
            };
            _dbContext.UserScheduleConfigs.Add(config);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        return new UserScheduleConfigDto(
            config.DynamicSchedulerEnabled,
            config.MaxWorkoutsPerDay,
            config.MinMuscleRestHours,
            config.RestDays,
            config.CycleWindowLengthDays,
            config.CycleStartDate
        );
    }
}