using System.Net;
using System.Reflection.Metadata;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Domain.Users;
using OptiLifts.Domain.Workouts;
using OptiLifts.Infrastructure.Database;
using FluentAssertions;

namespace OptiLifts.Tests.Api.Tests.Controllers;
public sealed class GoogleCalendarControllerTests
{
    private static async Task<OptiLiftsDbContext> CreateDbContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new OptiLiftsDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
    private static GoogleCalendarController CreateController(OptiLiftsDbContext db, IGoogleCalendarService calendarService, Guid userId)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        return new GoogleCalendarController(db, calendarService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            }
        };
    }

    [Fact]
    public async Task GetSettings_ShouldReturnDisconnected_WhenTokenisNull()
    {
        using var db = await CreateDbContextAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "test@example.com",
            EmailHash = "test-hash",
            PasswordHash = "x",
            DisplayName = "Test"
        });
        await db.SaveChangesAsync();
        
        var mockService = new Mock<IGoogleCalendarService>();
        var controller = CreateController(db, mockService.Object, userId);
        var result = await controller.GetSettings(CancellationToken.None);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value.Should().BeOfType<GoogleCalendarController.CalendarSettingsResponse>().Subject;
        value.isConnected.Should().BeFalse();
        value.SyncEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task ConnectCalendar_ShouldResetEntriesAndSync_WhenValidCode()
    {
        using var db = await CreateDbContextAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "test2@example.com",
            EmailHash = "test-hash",
            PasswordHash = "x",
            DisplayName = "Test"
        });
        var workout = new Workout
        {
            Id = Guid.NewGuid(),
            Name = "Leg Day",
            CreatedBy = userId
        };
        db.Workouts.Add(workout);
        var entry = new ScheduledEntry
        {
            Id = Guid.NewGuid(),
            WorkoutId = workout.Id,
            UserId = userId,
            Scheduled = DateTime.UtcNow.AddDays(1),
            Status = ScheduleStatus.Completed
        };
        db.ScheduledEntries.Add(entry);
        
        await db.SaveChangesAsync();

        var mockService = new Mock<IGoogleCalendarService>();
        mockService.Setup(s => s.ExchangeCodeForRefreshTokenAsync("valid_code", "postmessage", It.IsAny<CancellationToken>()))
        .ReturnsAsync("mock_refresh_token");
        mockService.Setup(s => s.GetOrCreateOptiLiftsCalendarIdAsync("mock_refresh_token", It.IsAny<CancellationToken>()))
        .ReturnsAsync("calendar_1");
        mockService.Setup(s => s.CreateEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GoogleCalendarEventDto>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync("new_event_id");

        var controller = CreateController(db, mockService.Object, userId);
        var request = new GoogleCalendarController.ConnectCalendarRequest("valid_code", "postmessage");
        var response = await controller.ConnectCalendar(request, CancellationToken.None);

        response.Should().BeOfType<OkObjectResult>();
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.GoogleCalendarRefreshToken.Should().Be("mock_refresh_token");
        user.GoogleCalendarId.Should().Be("calendar_1");
        user.GoogleCalendarSyncEnabled.Should().BeTrue();
        var updatedEntry = await db.ScheduledEntries.FirstAsync(e => e.Id == entry.Id);
        updatedEntry.GoogleEventId.Should().Be("new_event_id");
    }

    [Fact]
    public async Task DisconnectCalendar_ShouldClearTokensAndDisableSync()
    {
        using var db = await CreateDbContextAsync();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Email = "test3@example.com",
            EmailHash = "test-hash",
            PasswordHash = "x",
            DisplayName = "Test",
            GoogleCalendarRefreshToken = "token",
            GoogleCalendarId = "calender_2",
            GoogleCalendarSyncEnabled = true
        });
        await db.SaveChangesAsync();
        
        var mockService = new Mock<IGoogleCalendarService>();
        var controller = CreateController(db, mockService.Object, userId);
        var result = await controller.DisconnectCalendar(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.GoogleCalendarRefreshToken.Should().BeNull();
        user.GoogleCalendarId.Should().BeNull();
        user.GoogleCalendarSyncEnabled.Should().BeFalse();
    }

}