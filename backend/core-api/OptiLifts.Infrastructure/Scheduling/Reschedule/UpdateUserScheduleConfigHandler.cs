using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Scheduling.Reschedule;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Scheduling.Reschedule;

public class UpdateUserScheduleConfigHandler : IRequestHandler<UpdateUserScheduleConfigCommand, UserScheduleConfigDto>
{
    private readonly OptiLiftsDbContext _dbContext;
    public UpdateUserScheduleConfigHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserScheduleConfigDto> Handle(UpdateUserScheduleConfigCommand request, CancellationToken cancellationToken)
    {
        var config = await _dbContext.UserScheduleConfigs.FirstOrDefaultAsync(c => c.UserId ==request.UserId, cancellationToken);
        if (config == null)
        {
            config = new UserScheduleConfig
            {
                UserId = request.UserId
            };
            _dbContext.UserScheduleConfigs.Add(config);
        }
        config.DynamicSchedulerEnabled = request.Config.DynamicSchedulerEnabled;
        config.MaxWorkoutsPerDay = request.Config.MaxWorkoutsPerDay;
        config.MinMuscleRestHours = request.Config.MinMuscleRestHours;
        config.RestDays = request.Config.RestDays ?? new List<string>();
        config.CycleWindowLengthDays = request.Config.CycleWindowLengthDays;
        config.CycleStartDate = DateTime.SpecifyKind(request.Config.CycleStartDate.Date, DateTimeKind.Utc);

        await _dbContext.SaveChangesAsync(cancellationToken);

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