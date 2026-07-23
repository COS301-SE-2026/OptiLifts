using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Profile;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Profile;

public sealed class GetProfileOverviewHandler : IRequestHandler<GetProfileOverviewQuery, ProfileOverviewDto>
{
    private const int RecentWorkoutCount = 2;
    private const int ChartWindowWeeks = 12;

    private readonly OptiLiftsDbContext _dbContext;

    public GetProfileOverviewHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProfileOverviewDto> Handle(GetProfileOverviewQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(currentUser => currentUser.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException();
        }

        var completedSessions = await LoadCompletedSessionsAsync(request.UserId, cancellationToken);
        return await BuildSessionProfileAsync(user, completedSessions, cancellationToken);
    }

    private async Task<ProfileOverviewDto> BuildSessionProfileAsync(
        User user,
        IReadOnlyList<SessionRow> sessions,
        CancellationToken cancellationToken)
    {
        var recentSessions = sessions
            .GroupBy(session => session.WorkoutId)
            .Select(group => group.First())
            .Take(RecentWorkoutCount)
            .ToArray();
        var recentWorkoutIds = recentSessions.Select(session => session.WorkoutId).Distinct().ToArray();
        var recentLogIds = recentSessions.Select(session => session.LogId).ToArray();

        var workoutExercises = await LoadWorkoutExercisesAsync(recentWorkoutIds, cancellationToken);
        var workoutSets = await LoadWorkoutSetsAsync(recentWorkoutIds, cancellationToken);
        var recentLogSets = await LoadWorkoutLogSetsAsync(recentLogIds, cancellationToken);

        var recentWorkouts = recentSessions.Select(session =>
        {
            var exerciseNames = workoutExercises
                .Where(entry => entry.WorkoutId == session.WorkoutId)
                .Select(entry => entry.Name)
                .Distinct()
                .ToArray();

            var sessionSets = recentLogSets.Where(entry => entry.LogId == session.LogId).ToArray();
            var plannedSets = workoutSets.Where(entry => entry.WorkoutId == session.WorkoutId).ToArray();
            var sessionVolume = sessionSets.Length > 0
                ? sessionSets.Sum(entry => (double)entry.Reps * entry.Weight)
                : plannedSets.Sum(entry => (double)(entry.Reps ?? 0) * (entry.Weight ?? 0));
            var sessionSetCount = sessionSets.Length > 0 ? sessionSets.Length : plannedSets.Length;

            return new ProfileWorkoutDto(
                session.WorkoutId,
                session.LogId,
                session.WorkoutName,
                exerciseNames,
                $"{Math.Max(1, exerciseNames.Length)} PRs",
                FormatDuration(session.CompletedAt - session.StartedAt),
                FormatWeight(sessionVolume),
                $"{sessionSetCount} sets");
        }).ToArray();

        var totalSessions = sessions.Count;
        var totalLoggedSets = await CountWorkoutLogSetsAsync(sessions.Select(session => session.LogId).ToArray(), cancellationToken);
        var streakWeeks = ComputeStreakWeeks(sessions.Select(session => session.CompletedAt));

        var badges = new List<ProfileBadgeDto>();

        if (totalSessions > 0)
        {
            int bestMilestone = 0;
            int[] milestones = { 1, 5, 10, 25, 50, 100, 250, 500 };
            foreach (var milestone in milestones)
                if (totalSessions >= milestone && totalSessions > bestMilestone)
                    bestMilestone = milestone;
            badges.Add(new ProfileBadgeDto($"{bestMilestone} WORKOUTS", $"Completed {bestMilestone} workouts", "MILESTONE", sessions[totalSessions - bestMilestone].CompletedAt));
        }

        switch (streakWeeks)
        {
            case >= 104:
                badges.Add(new ProfileBadgeDto("IRON MAN", $"Maintained a {streakWeeks / 52} year-long streak", "STREAK", DateTime.UtcNow));
                break;
            case >= 52:
                badges.Add(new ProfileBadgeDto("YEARLONG", "Maintained a year-long streak", "STREAK", DateTime.UtcNow));
                break;
            case >= 24:
                badges.Add(new ProfileBadgeDto("HALFYEAR", "Maintained a half-year-long streak", "STREAK", DateTime.UtcNow));
                break;
            case >= 12:
                badges.Add(new ProfileBadgeDto("QUARTERLONG", "Maintained a quarter-long streak", "STREAK", DateTime.UtcNow));
                break;
            case >= 4:
                badges.Add(new ProfileBadgeDto("MONTHLONG", "Maintained a month-long streak", "STREAK", DateTime.UtcNow));
                break;
        }

        switch (totalLoggedSets)
        {
            case >= 5000:
                badges.Add(new ProfileBadgeDto("5000 SETS", $"Logged over 5000 sets", "SET COUNT", DateTime.UtcNow));
                break;
            case >= 2500:
                badges.Add(new ProfileBadgeDto("2500 SETS", $"Logged over 2500 sets", "SET COUNT", DateTime.UtcNow));
                break;
            case >= 1000:
                badges.Add(new ProfileBadgeDto("1000 SETS", $"Logged over 1000 sets", "SET COUNT", DateTime.UtcNow));
                break;
            case >= 500:
                badges.Add(new ProfileBadgeDto("500 SETS", $"Logged over 500 sets", "SET COUNT", DateTime.UtcNow));
                break;
            case >= 100:
                badges.Add(new ProfileBadgeDto("100 SETS", $"Logged over 100 sets", "SET COUNT", DateTime.UtcNow));
                break;
        }

        var chartData = BuildWeeklyChartData(sessions.Select(session => new WeeklyDurationSample(session.CompletedAt, (session.CompletedAt - session.StartedAt).TotalHours)), ChartWindowWeeks, "Weekly Hours");

        return new ProfileOverviewDto(
            new ProfileUserDto(user.DisplayName, user.Email, user.Bio, user.ProfileImageUrl),
            badges,
            recentWorkouts,
            chartData.Title,
            chartData.Points);
    }

    private async Task<IReadOnlyList<SessionRow>> LoadCompletedSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (
            from log in _dbContext.WorkoutLogs.AsNoTracking()
            where log.EntryId.HasValue && log.CompletedAt != null
            join entry in _dbContext.ScheduledEntries.AsNoTracking() on log.EntryId!.Value equals entry.Id
            where entry.UserId == userId
            join workout in _dbContext.Workouts.AsNoTracking() on entry.WorkoutId equals workout.Id
            orderby log.CompletedAt descending
            select new SessionRow(
                log.Id,
                entry.WorkoutId,
                workout.Name,
                log.StartedAt,
                log.CompletedAt!.Value))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<WorkoutRow>> LoadRecentWorkoutsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _dbContext.Workouts
            .AsNoTracking()
            .Where(workout => workout.CreatedBy == userId)
            .OrderByDescending(workout => workout.CreatedAt)
            .Select(workout => new WorkoutRow(workout.Id, workout.Name, workout.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<WorkoutExerciseRow>> LoadWorkoutExercisesAsync(Guid[] workoutIds, CancellationToken cancellationToken)
    {
        if (workoutIds.Length == 0)
        {
            return Array.Empty<WorkoutExerciseRow>();
        }

        return await (
            from workoutExercise in _dbContext.WorkoutExercises.AsNoTracking()
            where workoutIds.Contains(workoutExercise.WorkoutId)
            join exercise in _dbContext.Exercises.AsNoTracking() on workoutExercise.ExerciseId equals exercise.Id
            select new WorkoutExerciseRow(workoutExercise.WorkoutId, workoutExercise.ExerciseId, exercise.Name))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<WorkoutSetRow>> LoadWorkoutSetsAsync(Guid[] workoutIds, CancellationToken cancellationToken)
    {
        if (workoutIds.Length == 0)
        {
            return Array.Empty<WorkoutSetRow>();
        }

        return await (
            from workoutSet in _dbContext.Sets.AsNoTracking()
            join workoutExercise in _dbContext.WorkoutExercises.AsNoTracking() on workoutSet.WorkoutExerciseId equals workoutExercise.Id
            where workoutIds.Contains(workoutExercise.WorkoutId)
            select new WorkoutSetRow(
                workoutExercise.WorkoutId,
                workoutSet.Reps,
                workoutSet.Weight,
                workoutSet.RestTime))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<WorkoutLogSetRow>> LoadWorkoutLogSetsAsync(Guid[] logIds, CancellationToken cancellationToken)
    {
        if (logIds.Length == 0)
        {
            return Array.Empty<WorkoutLogSetRow>();
        }

        return await (
            from workoutSetLog in _dbContext.WorkoutLogSets.AsNoTracking()
            where logIds.Contains(workoutSetLog.LogId)
            join exercise in _dbContext.Exercises.AsNoTracking() on workoutSetLog.ExerciseId equals exercise.Id
            select new WorkoutLogSetRow(
                workoutSetLog.LogId,
                workoutSetLog.ExerciseId,
                exercise.Name,
                workoutSetLog.Reps,
                workoutSetLog.Weight))
            .ToListAsync(cancellationToken);
    }

    private async Task<int> CountWorkoutLogSetsAsync(Guid[] logIds, CancellationToken cancellationToken)
    {
        if (logIds.Length == 0)
        {
            return 0;
        }

        return await _dbContext.WorkoutLogSets
            .AsNoTracking()
            .CountAsync(workoutSetLog => logIds.Contains(workoutSetLog.LogId), cancellationToken);
    }

    private static int ComputeStreakWeeks(IEnumerable<DateTime> dates)
    {
        var weekStarts = new HashSet<DateTime>(dates.Select(GetWeekStart));
        var streak = 0;
        var currentWeek = GetWeekStart(DateTime.UtcNow);

        while (weekStarts.Contains(currentWeek))
        {
            streak++;
            currentWeek = currentWeek.AddDays(-7);
        }

        return streak;
    }

    private static ProfileChart BuildWeeklyChartData(IEnumerable<WeeklyDurationSample> samples, int weekCount, string title)
    {
        var valuesByWeek = samples
            .Select(sample => new { Week = GetWeekStart(sample.Date), sample.Value })
            .GroupBy(sample => sample.Week)
            .ToDictionary(group => group.Key, group => group.Sum(sample => sample.Value));

        var currentWeek = GetWeekStart(DateTime.UtcNow);
        var points = Enumerable.Range(0, weekCount)
            .Select(offset => currentWeek.AddDays(-(weekCount - 1 - offset) * 7))
            .Select(weekStart => new ProfileChartDatumDto(
                weekStart.ToString("MMM d", CultureInfo.InvariantCulture),
                valuesByWeek.TryGetValue(weekStart, out var value) ? value : 0))
            .ToArray();

        return new ProfileChart(title, points);
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var utcDate = date.Kind == DateTimeKind.Utc ? date.Date : date.ToUniversalTime().Date;
        var offset = ((int)utcDate.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return utcDate.AddDays(-offset);
    }

    private static string EstimateDuration(IReadOnlyList<WorkoutSetRow> sets)
    {
        if (sets.Count == 0)
        {
            return "0m";
        }

        var activeMinutes = sets.Count * 4;
        var restMinutes = sets.Sum(set => set.RestTime) / 60;
        return FormatDuration(TimeSpan.FromMinutes(Math.Max(20, activeMinutes + restMinutes)));
    }

    private static string FormatWeight(double volume)
    {
        var rounded = Math.Round(volume);
        return $"{rounded.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ')} kg";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        var totalMinutes = Math.Max(0, (int)Math.Round(duration.TotalMinutes));
        if (totalMinutes < 60)
        {
            return totalMinutes == 0 ? "<1m" : $"{totalMinutes}m";
        }

        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return minutes == 0 ? $"{hours}h" : $"{hours}h {minutes}m";
    }

    private sealed record SessionRow(Guid LogId, Guid WorkoutId, string WorkoutName, DateTime StartedAt, DateTime CompletedAt);

    private sealed record WorkoutRow(Guid WorkoutId, string Name, DateTime CreatedAt);

    private sealed record WorkoutExerciseRow(Guid WorkoutId, Guid ExerciseId, string Name);

    private sealed record WorkoutSetRow(Guid WorkoutId, int? Reps, float? Weight, int RestTime);

    private sealed record WorkoutLogSetRow(Guid LogId, Guid ExerciseId, string Name, int Reps, float Weight);

    private sealed record WeeklyDurationSample(DateTime Date, double Value);

    private sealed record ProfileChart(string Title, IReadOnlyList<ProfileChartDatumDto> Points);
}