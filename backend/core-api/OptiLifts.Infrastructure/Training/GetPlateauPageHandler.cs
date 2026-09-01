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
        var recencyCutoff = DateTime.UtcNow.AddDays(-RecencyCutoffDays);

        var rows = await (
            from trend in _dbContext.ExerciseTrends.AsNoTracking()
            join exercise in _dbContext.Exercises.AsNoTracking() on trend.ExerciseId equals exercise.Id
            where trend.UserId == request.UserId
                && (trend.Status == TrendStatus.Plateau || trend.Status == TrendStatus.Regressing || trend.Status == TrendStatus.Progressing)
                && trend.WindowEnd >= recencyCutoff
            select new { trend, exercise.Name }
        ).ToListAsync(cancellationToken);

        return rows
            .Select(r => new ExerciseDiagnosisDto(
                r.trend.ExerciseId,
                r.Name,
                r.trend.Status,
                r.trend.SlopePctPerWeek,
                BuildRecommendation(r.trend.Status, r.trend.RpeTrendRising),
                r.trend.ComputedAt))
            .ToList();
    }

    private static string? BuildRecommendation(TrendStatus status, bool rpeTrendRising)
    {
        if (status == TrendStatus.Progressing)
        {
            return null;
        }

        return rpeTrendRising
            ? "Your efforts have been increasing but your lifts are stalling. Prioritise sleep, nutrition or recovery."
            : "Try changing this exercise or adjusting your rep range. A change of stimulus could be your solution.";
    }
}
