using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OptiLifts.Application.Users;
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
            )
        };
    }
}