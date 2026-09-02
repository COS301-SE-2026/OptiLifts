using Microsoft.EntityFrameworkCore;
using OptiLifts.Domain.Training;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Training;

public interface IPlateauDetectionService
{
    Task DetectAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken);
}

public sealed class PlateauDetectionService : IPlateauDetectionService
{
    private const int sizeOfWindow = 12;
    private const int MaxGapInDays = 14;
    private const int BaselineDays = 84;
    private const int MinBaselineSpan = 56;
    private const int MinBaselinePts = 4;
    private const float GapThreshold = 2.5f;
    private const float DefEpsi = 0.5f;
    private const float EpsiFactor = 0.3f;
    private const float MinEpsi = 0.3f;
    private const float MaxEpsi = 1.5f;
    private const double TCritical = 2.228;
    private const int LookbackDays = 400;
    private const int EventCooldownDays = 14;

    private readonly ISeriesBuilder _seriesBuilder;
    private readonly OptiLiftsDbContext _dbContext;

    public PlateauDetectionService(ISeriesBuilder seriesBuilder, OptiLiftsDbContext dbContext)
    {
        _seriesBuilder = seriesBuilder;
        _dbContext = dbContext;
    }

    public async Task DetectAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken)
    {
        var series = await _seriesBuilder.BuildAsync(userId, exerciseId, DateTime.UtcNow.AddDays(-LookbackDays), cancellationToken);

        var alreadyExists = await _dbContext.ExerciseTrends.FirstOrDefaultAsync(t => t.UserId == userId && t.ExerciseId == exerciseId, cancellationToken);

        if (series.Count < sizeOfWindow)
        {
            await UpsertAsync(alreadyExists, userId, exerciseId, TrendStatus.InsufficientData, 0, 0, 0, 0, series.Count,
                series.Count > 0 ? series[0].Date : DateTime.UtcNow,
                series.Count > 0 ? series[^1].Date : DateTime.UtcNow,
                false, cancellationToken);
            return;
        }

        var wndw = series.TakeLast(sizeOfWindow).ToList();

        for (var i = 1; i < wndw.Count; i++)
        {
            if ((wndw[i].Date - wndw[i - 1].Date).TotalDays > MaxGapInDays)
            {
                await UpsertAsync(alreadyExists, userId, exerciseId, TrendStatus.InsufficientData, 0, 0, 0, 0, wndw.Count, wndw[0].Date, wndw[^1].Date, false, cancellationToken);
                return;
            }
        }

        var wndwStart = wndw[0].Date;
        var xs = wndw.Select(p => (p.Date - wndwStart).TotalDays).ToArray();
        var ys = wndw.Select(p => (double)p.E1rm).ToArray();

        var (aWindow, bWindow) = LeastSqLineFit(xs, ys);
        var meanE1rmWindow = ys.Average();
        var slopePercPerWeek = bWindow * 7 / meanE1rmWindow * 100;

        var (ciLow, ciHigh) = CompConfidenceInter(xs, ys, aWindow, bWindow, meanE1rmWindow);


        var rpePoints = wndw.Where(p => p.AvgRpe.HasValue).ToList();
        var rpeTrendRising = false;
        if (rpePoints.Count >= 4)
        {
            var rpeOrigin = rpePoints[0].Date;
            var rpeXs = rpePoints.Select(p => (p.Date - rpeOrigin).TotalDays).ToArray();
            var rpeYs = rpePoints.Select(p => (double)p.AvgRpe!.Value).ToArray();
            var (_, rpeSlope) = LeastSqLineFit(rpeXs, rpeYs);
            rpeTrendRising = rpeSlope * 7 > 0.3;
        }

        var baselinePts = series.Where(p => p.Date < wndwStart && p.Date >= wndwStart.AddDays(-BaselineDays)).ToList();
        var baselineAvail = baselinePts.Count >= MinBaselinePts && (baselinePts[^1].Date - baselinePts[0].Date).TotalDays >= MinBaselineSpan;

        float epsi;
        double aBase = 0, bBase = 0;
        var baselineOrig = default(DateTime);

        if (baselineAvail)
        {
            baselineOrig = baselinePts[0].Date;
            var bxs = baselinePts.Select(p => (p.Date - baselineOrig).TotalDays).ToArray();
            var bys = baselinePts.Select(p => (double)p.E1rm).ToArray();

            (aBase, bBase) = LeastSqLineFit(bxs, bys);

            var meanBase = bys.Average();
            var baselineRatePctWeek = bBase * 7 / meanBase * 100;

            epsi = Math.Clamp((float)(EpsiFactor * baselineRatePctWeek), MinEpsi, MaxEpsi);
        }
        else
        {
            epsi = DefEpsi;
        }

        TrendStatus currStatus;

        if (!baselineAvail)
        {
            if (slopePercPerWeek > epsi) currStatus = TrendStatus.Progressing;

            else if (slopePercPerWeek < -epsi) currStatus = TrendStatus.Regressing;

            else currStatus = TrendStatus.InsufficientBaseline;
        }
        else
        {
            var predicted = wndw.Select(p => aBase + bBase * (p.Date - baselineOrig).TotalDays).ToArray();
            var residualPerc = wndw.Select((p, i) => (p.E1rm - predicted[i]) / predicted[i] * 100).ToArray();
            var tailCount = Math.Max(3, sizeOfWindow / 2);
            var gap = residualPerc.TakeLast(tailCount).Average();

            if (gap > GapThreshold) currStatus = TrendStatus.Progressing;

            else if (gap < -GapThreshold) currStatus = slopePercPerWeek < -epsi ? TrendStatus.Regressing : TrendStatus.Plateau;

            else currStatus = TrendStatus.Progressing;
        }

        var confirmed = alreadyExists is not null && alreadyExists.Status == currStatus;

        await UpsertAsync(alreadyExists, userId, exerciseId, currStatus, slopePercPerWeek, ciLow, ciHigh, meanE1rmWindow, wndw.Count, wndwStart, wndw[^1].Date, rpeTrendRising, cancellationToken);

        if (confirmed && currStatus == TrendStatus.Plateau)
        {
            await RecordPlatEventAsync(userId, exerciseId, cancellationToken);
        }
    }

    private async Task UpsertAsync(ExerciseTrend? existing, Guid userId, Guid exerciseId, TrendStatus status,
        double slopePctPerWeek, double ciLow, double ciHigh, double meanE1rm, int sessionsUsed, DateTime windowStart, DateTime windowEnd, bool rpeTrendRising, CancellationToken cancellationToken)
    {
        if (existing is null)
        {
            existing = new ExerciseTrend { UserId = userId, ExerciseId = exerciseId };
            _dbContext.ExerciseTrends.Add(existing);
        }

        existing.Status = status;
        existing.SlopePctPerWeek = (float)slopePctPerWeek;
        existing.SlopeCiLow = (float)ciLow;
        existing.SlopeCiHigh = (float)ciHigh;
        existing.MeanE1rm = (float)meanE1rm;
        existing.SessionsUsed = sessionsUsed;
        existing.WindowStart = windowStart;
        existing.WindowEnd = windowEnd;
        existing.RpeTrendRising = rpeTrendRising;
        existing.ComputedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RecordPlatEventAsync(Guid userId, Guid exerciseId, CancellationToken cancellationToken)
    {
        var cdStart = DateTime.UtcNow.AddDays(-EventCooldownDays);
        var recentRecorded = await _dbContext.TrainingEvents
            .AnyAsync(e => e.UserId == userId && e.Type == TrainingEventType.PlateauDetected
                && e.Scope == exerciseId.ToString() && e.CreatedAt >= cdStart, cancellationToken);

        if (recentRecorded)
        {
            return;
        }

        var exer = await _dbContext.Exercises.AsNoTracking().FirstOrDefaultAsync(e => e.Id == exerciseId, cancellationToken);

        _dbContext.TrainingEvents.Add(new TrainingEvent
        {
            UserId = userId,
            Type = TrainingEventType.PlateauDetected,
            Scope = exerciseId.ToString(),
            Diagnosis = $"Plateau detected on {exer?.Name ?? "exercise"}"
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static (double a, double b) LeastSqLineFit(double[] xs, double[] ys)
    {
        var n = xs.Length;
        var meanX = xs.Average();
        var meanY = ys.Average();
        var sxx = xs.Sum(x => (x - meanX) * (x - meanX));
        var sxy = xs.Zip(ys, (x, y) => (x - meanX) * (y - meanY)).Sum();
        var b = sxy / sxx;
        var a = meanY - b * meanX;

        return (a, b);
    }

    private static (double low, double high) CompConfidenceInter(double[] xs, double[] ys, double a, double b, double meanY)
    {
        var n = xs.Length;
        var meanX = xs.Average();
        var sxx = xs.Sum(x => (x - meanX) * (x - meanX));
        var residualSumSquares = xs.Zip(ys, (x, y) => Math.Pow(y - (a + b * x), 2)).Sum();
        var seB = Math.Sqrt(residualSumSquares / (n - 2) / sxx);
        var half = TCritical * seB;
        var slopePctPerWeek = b * 7 / meanY * 100;
        var halfPctPerWeek = half * 7 / meanY * 100;

        return (slopePctPerWeek - halfPctPerWeek, slopePctPerWeek + halfPctPerWeek);
    }
}
