using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Training.GetPlateauPage;
using OptiLifts.Domain.Training;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Training;

public sealed class GetPlateauPageHandler : IRequestHandler<GetPlateauPageQuery, IReadOnlyList<ExerciseDiagnosisDto>>
{
    private readonly OptiLiftsDbContext _dbContext;

    public GetPlateauPageHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private const int RecencyCutoffDays = 30;

    public async Task<IReadOnlyList<ExerciseDiagnosisDto>> Handle(GetPlateauPageQuery request, CancellationToken cancellationToken)
    {
        var monthCutOff = DateTime.UtcNow.AddDays(-RecencyCutoffDays);

        var rows = await (
            from trend in _dbContext.ExerciseTrends.AsNoTracking()
            join exercise in _dbContext.Exercises.AsNoTracking() on trend.ExerciseId equals exercise.Id
            join muscle in _dbContext.Muscles.AsNoTracking() on exercise.PrimaryMuscleId equals muscle.Id
            where trend.UserId == request.UserId
                && (trend.Status == TrendStatus.Plateau || trend.Status == TrendStatus.Regressing || trend.Status == TrendStatus.Progressing)
                && trend.WindowEnd >= monthCutOff
            select new { trend, exercise.Name, MuscleName = muscle.Name }
        ).ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var exerIds = rows.Select(r => r.trend.ExerciseId).Distinct().ToArray();

        var workoutRows = await (
            from we in _dbContext.WorkoutExercises.AsNoTracking()
            join workout in _dbContext.Workouts.AsNoTracking() on we.WorkoutId equals workout.Id
            where exerIds.Contains(we.ExerciseId) && workout.CreatedBy == request.UserId && !workout.IsDeleted
            select new { we.ExerciseId, workout.Id, workout.Name }
        ).ToListAsync(cancellationToken);

        var workoutsByExer = workoutRows
            .GroupBy(w => w.ExerciseId).ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<WorkoutRefDto>)g.Select(w => new WorkoutRefDto(w.Id, w.Name)).Distinct().ToList());
        
        return rows
            .Select(r => new ExerciseDiagnosisDto(
                r.trend.ExerciseId,
                r.Name,
                r.MuscleName,
                r.trend.Status,
                r.trend.SlopePctPerWeek,
                BuildRecommendation(r.trend.Status, r.trend.RpeTrendRising),
                r.trend.Status != TrendStatus.Progressing && !r.trend.RpeTrendRising,
                r.trend.ComputedAt,
                workoutsByExer.TryGetValue(r.trend.ExerciseId, out var workouts) ? workouts : []))
            .ToList();

    }

    private static string? BuildRecommendation(TrendStatus status, bool rpeTrendRising)
    {
        if (status == TrendStatus.Progressing)
        {
            return null;
        }

        return rpeTrendRising
            ? "Your effort has been climbing while progress has stalled. Prioritise sleep, nutrition and workout consistency before pushing harder on this exercise."
            : "Only your progress is stalling. Try changing this exercise or adjusting your rep range for a change of stimulus";
    }
}
