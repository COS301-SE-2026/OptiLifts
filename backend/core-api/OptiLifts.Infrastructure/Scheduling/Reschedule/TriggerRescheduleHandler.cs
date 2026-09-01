using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using OptiLifts.Application.Scheduling.Reschedule;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Scheduling.Reschedule;

public class TriggerRescheduleHandler : IRequestHandler<TriggerRescheduleCommand, RescheduleResultDto>
{
    private readonly OptiLiftsDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;
    public TriggerRescheduleHandler(OptiLiftsDbContext dbContext, IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<RescheduleResultDto> Handle(TriggerRescheduleCommand request, CancellationToken cancellationToken)
    {
        var config = await _dbContext.UserScheduleConfigs.AsNoTracking().FirstOrDefaultAsync(c => c.UserId ==request.UserId, cancellationToken);

        var cycleLength = config?.CycleWindowLengthDays ?? 7;//default to a cycle of a week
        var cycleStartDate = config?.CycleStartDate ?? DateTime.UtcNow.Date;
        //calcul to get current cycle
        var today = DateTime.UtcNow.Date;
        var dayssinceStart =Math.Max(0, (today - cycleStartDate).Days);
        var cycleIndex = dayssinceStart/cycleLength;
        var start = DateTime.SpecifyKind(cycleStartDate.AddDays(cycleIndex * cycleLength), DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(start.AddDays(cycleLength).AddTicks(-1), DateTimeKind.Utc);

        //get the entries in zis window
        var entries = await _dbContext.ScheduledEntries.AsNoTracking()
        .Where(e => e.UserId == request.UserId && e.Scheduled >= start && e.Scheduled <= end)
        .ToListAsync(cancellationToken);

        //filter the users selected missed and the upcoming
        var targetentries = entries.Where(e => (e.Status == ScheduleStatus.Missed && request.SelectedMissedEntryIds.Contains(e.Id)) 
        ||  e.Status == ScheduleStatus.Scheduled)
        .ToList();
        if (targetentries.Count == 0)
        {
            return new RescheduleResultDto(request.UserId, "None", 0, new List<RescheduledEntryDto>(), new List<RescheduleEntryDetailDto>());
        }

        //fetch workouots and their primary muscles
        var workoutIds = targetentries.Select(e => e.WorkoutId).Distinct().ToList();
        var workoutNames = await _dbContext.Workouts.AsNoTracking()
        .Where(w => workoutIds.Contains(w.Id))
        .ToDictionaryAsync(w => w.Id, w=> w.Name, cancellationToken);
        var workoutMuscles = await (
            from we in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutIds.Contains(we.WorkoutId)
            join ex in _dbContext.Exercises.AsNoTracking() on we.ExerciseId equals ex.Id
            join m in _dbContext.Muscles.AsNoTracking() on ex.PrimaryMuscleId equals m.Id
            into muscleGroup
            from m in muscleGroup.DefaultIfEmpty()
            select new
            {
                we.WorkoutId,
                MuscleName = m != null? m.Name : null
            }
        ).ToListAsync(cancellationToken);
        var musclesWorkout = workoutMuscles.Where(x => !string.IsNullOrEmpty(x.MuscleName))
        .GroupBy(x=> x.WorkoutId)
        .ToDictionary(g => g.Key, g=> g.Select(x => x.MuscleName!).Distinct().ToList());

        //make the python req
        var pythonEntries = targetentries.Select(e => new PythonEntry(
            e.Id.ToString(),
            e.WorkoutId.ToString(),
            workoutNames.GetValueOrDefault(e.WorkoutId, "Workout"),
            e.Scheduled,
            e.Status.ToString(),
            musclesWorkout.GetValueOrDefault(e.WorkoutId, new List<string>())
        )).ToList();
        var effectiveStart = start < today ? today : start;
        var payload = new PythonRescheduleRequest(
            request.UserId.ToString(),
            effectiveStart,
            end,
            new PythonPreferences(
                config?.MaxWorkoutsPerDay ?? 1,
                config?.MinMuscleRestHours ?? 48,
                config?.RestDays ?? new List<string>{"Sunday"}
            ),
            pythonEntries
        );
        var snakecase = new JsonSerializerOptions{PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower};

        var client = _httpClientFactory.CreateClient("AiApi");
        var response = await client.PostAsJsonAsync("ai-api/reschedule", payload, snakecase, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PythonRescheduleResponse>(snakecase, cancellationToken: cancellationToken);
        if (result == null)
        {
            throw new InvalidOperationException("Invalid response from Python ai");
        }

        return new RescheduleResultDto(
            request.UserId,
            result.ExecutionTier,
            result.ExecutionTimeMs,
            result.RescheduledEntries.Select(r => new RescheduledEntryDto(
                Guid.Parse(r.EntryId),
                Guid.Parse(r.WorkoutId),
                r.WorkoutName,
                r.OriginalScheduledAt,
                r.NewScheduledAt,
                r.Action
            )).ToList(),
            result.DroppedEntries.Select(d => new RescheduleEntryDetailDto(
                Guid.Parse(d.Id),
                Guid.Parse(d.WorkoutId),
                d.WorkoutName,
                d.ScheduledAt,
                d.Status,
                d.PrimaryMuscles
            )).ToList()
        );
    }
}