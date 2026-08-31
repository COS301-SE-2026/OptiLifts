using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;

namespace OptiLifts.Infrastructure.Users;

public sealed class UserSettingsHandler : IRequestHandler<GetUserSettingsQuery, UserSettingsDto>
{
    private readonly OptiLiftsDbContext _dbContext;
    public UserSettingsHandler(OptiLiftsDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task<UserSettingsDto> Handle(GetUserSettingsQuery request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var repRanges = await _dbContext.UserRepRanges
            .Where(r => r.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        if (repRanges.Count == 0)
        {
            var defaultCompound = new UserRepRange
            {
                UserId = request.UserId,
                ExerciseType = UserRepRangeExerciseType.Compound,
                LowerLimit = 5,
                UpperLimit = 8
            };

            var defaultIsolation = new UserRepRange
            {
                UserId = request.UserId,
                ExerciseType = UserRepRangeExerciseType.Isolation,
                LowerLimit = 8,
                UpperLimit = 12
            };
            _dbContext.UserRepRanges.AddRange(defaultCompound, defaultIsolation);
            await _dbContext.SaveChangesAsync(cancellationToken);
            repRanges.Add(defaultCompound);
            repRanges.Add(defaultIsolation);
        }

        double? weight = null;
        double? height = null;

        if (double.TryParse(user.Weight, NumberStyles.Any, CultureInfo.InvariantCulture, out var w))
        {
            weight = w;
        }

        if (double.TryParse(user.Height, NumberStyles.Any, CultureInfo.InvariantCulture, out var h))
        {
            height = h;
        }

        DateTime? dateOfBirth = null;
        if (DateTime.TryParse(user.DateOfBirth, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dob))
        {
            dateOfBirth = dob;
        }

        return new UserSettingsDto
        {
            Profile = new ProfileDto(
                user.DisplayName,
                user.Bio ?? string.Empty,
                user.Sex ?? "PreferNotToSay",
                dateOfBirth,
                weight,
                height,
                user.ProfileImageUrl
            ),
            Preferences = new PreferencesDto(
                user.LightTheme ? "light" : "dark",
                user.Metric ? "metric" : "imperial"
            ),
            RepRanges = repRanges.Select(r => new UserRepRangeDto(
                r.Id,
                r.ExerciseType.ToString(),
                r.LowerLimit,
                r.UpperLimit
            )).ToList(),
            Security = new SecurityDto(!string.IsNullOrWhiteSpace(user.PasswordHash))
        };
    }
}