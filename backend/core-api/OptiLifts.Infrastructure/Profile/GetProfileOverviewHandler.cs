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
        if (completedSessions.Count > 0)
        {
            return await BuildSessionProfileAsync(user, completedSessions, cancellationToken);
        }

        var workouts = await LoadRecentWorkoutsAsync(request.UserId, cancellationToken);
        return await BuildWorkoutProfileAsync(user, workouts, cancellationToken);
    }

    private async Task<ProfileOverviewDto> BuildSessionProfileAsync(
        User user,
        IReadOnlyList<SessionRow> sessions,
        CancellationToken cancellationToken)
    {
        var recentSessions = sessions.Take(RecentWorkoutCount).ToArray();
        var recentWorkoutIds = recentSessions.Select(session => session.WorkoutId).Distinct().ToArray();
        var recentLogIds = recentSessions.Select(session => session.LogId).ToArray();

        var workoutExercises = await LoadWorkoutExercisesAsync(recentWorkoutIds, cancellationToken);
        var workoutSets = await LoadWorkoutSetsAsync(recentWorkoutIds, cancellationToken);
        var recentLogSets = await LoadWorkoutLogSetsAsync(recentLogIds, cancellationToken);
        var badges = await LoadEarnedBadgesAsync(user.Id, cancellationToken);

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

        var chartData = BuildWeeklyChartData(
            sessions.Select(session => session.CompletedAt),
            ChartWindowWeeks,
            "Workout activity");

        return new ProfileOverviewDto(
            new ProfileUserDto(user.DisplayName, user.Email, user.Bio, user.ProfileImageUrl),
            new[]
            {
                new ProfileStatDto("Streak", $"{streakWeeks} weeks"),
                new ProfileStatDto("Workouts", $"{totalSessions} sessions"),
                new ProfileStatDto("Records", $"{totalLoggedSets:N0} logged sets"),
            },
            badges,
            recentWorkouts,
            chartData.Title,
            chartData.Points);
    }

    private async Task<ProfileOverviewDto> BuildWorkoutProfileAsync(
        User user,
        IReadOnlyList<WorkoutRow> workouts,
        CancellationToken cancellationToken)
    {
        var recentWorkouts = workouts.Take(RecentWorkoutCount).ToArray();
        var recentWorkoutIds = recentWorkouts.Select(workout => workout.WorkoutId).Distinct().ToArray();

        var workoutExercises = await LoadWorkoutExercisesAsync(recentWorkoutIds, cancellationToken);
        var workoutSets = await LoadWorkoutSetsAsync(recentWorkoutIds, cancellationToken);
        var badges = await LoadEarnedBadgesAsync(user.Id, cancellationToken);

        var workoutCards = recentWorkouts.Select(workout =>
        {
            var exerciseNames = workoutExercises
                .Where(entry => entry.WorkoutId == workout.WorkoutId)
                .Select(entry => entry.Name)
                .Distinct()
                .ToArray();

            var plannedSets = workoutSets.Where(entry => entry.WorkoutId == workout.WorkoutId).ToArray();
            var volume = plannedSets.Sum(entry => (double)(entry.Reps ?? 0) * (entry.Weight ?? 0));
            var duration = EstimateDuration(plannedSets);

            return new ProfileWorkoutDto(
                workout.Name,
                exerciseNames,
                $"{Math.Max(1, exerciseNames.Length)} PRs",
                duration,
                FormatWeight(volume),
                $"{plannedSets.Length} sets");
        }).ToArray();

        var streakWeeks = ComputeStreakWeeks(workouts.Select(workout => workout.CreatedAt));
        var totalWorkouts = workouts.Count;
        var totalExercises = workoutExercises
            .Select(entry => entry.ExerciseId)
            .Distinct()
            .Count();

        var chartData = BuildWeeklyChartData(
            workouts.Select(workout => workout.CreatedAt),
            ChartWindowWeeks,
            "Workout creation");

        return new ProfileOverviewDto(
            new ProfileUserDto(user.DisplayName, user.Email, user.Bio, user.ProfileImageUrl),
            new[]
            {
                new ProfileStatDto("Streak", $"{streakWeeks} weeks"),
                new ProfileStatDto("Workouts", $"{totalWorkouts} workouts"),
                new ProfileStatDto("Records", $"{totalExercises:N0} exercises tracked"),
            },
            badges,
            workoutCards,
            chartData.Title,
            chartData.Points);
    }

    private async Task<IReadOnlyList<ProfileBadgeDto>> LoadEarnedBadgesAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await (
            from userBadge in _dbContext.UserBadges.AsNoTracking()
            where userBadge.UserId == userId
            join badge in _dbContext.Badges.AsNoTracking() on userBadge.BadgeId equals badge.Id
            orderby userBadge.EarnedAt descending, badge.Name
            select new ProfileBadgeDto(
                badge.Name,
                badge.Description,
                badge.Category.ToString(),
                badge.IconUrl,
                userBadge.EarnedAt))
            .ToListAsync(cancellationToken);
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

    private static ProfileChart BuildWeeklyChartData(IEnumerable<DateTime> dates, int weekCount, string title)
    {
        var countsByWeek = dates
            .Select(GetWeekStart)
            .GroupBy(week => week)
            .ToDictionary(group => group.Key, group => group.Count());

        var currentWeek = GetWeekStart(DateTime.UtcNow);
        var points = Enumerable.Range(0, weekCount)
            .Select(offset => currentWeek.AddDays(-(weekCount - 1 - offset) * 7))
            .Select(weekStart => new ProfileChartDatumDto(
                weekStart.ToString("MMM d", CultureInfo.InvariantCulture),
                countsByWeek.TryGetValue(weekStart, out var count) ? count : 0))
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
            return $"{totalMinutes}m";
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

    private sealed record ProfileChart(string Title, IReadOnlyList<ProfileChartDatumDto> Points);
}