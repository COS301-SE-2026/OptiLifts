using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Clash.Arenas;
using OptiLifts.Application.Clash.Arenas.Commands;
using OptiLifts.Application.Clash.Arenas.Queries;
using Xunit;

namespace OptiLifts.Tests.Api.Tests.Controllers;

public sealed class ArenasControllerTests
{
    private const string AuthScheme = "TestAuth";

    private static ArenasController CreateController(Mock<ISender> sender, Guid? userId = null)
    {
        var controller = new ArenasController(sender.Object);
        var context = new DefaultHttpContext();
        if (userId.HasValue)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString())
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthScheme));
        }
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
        return controller;
    }

    private static ArenaDto CreateSampleArenaDto(string id = "test-arena", string name = "Test Squad", string code = "TEST01", string role = "Owner")
    {
        return new ArenaDto(
            Id: id,
            Name: name,
            Type: "Private",
            Code: code,
            CreatedById: Guid.NewGuid(),
            MetricType: "DotsOverall",
            DurationDays: 30,
            SeasonEndDate: DateTime.UtcNow.AddDays(30),
            CreatedAt: DateTime.UtcNow,
            MemberCount: 1,
            IsActive: true,
            DaysRemaining: 30,
            UserRole: role,
            IsUserMember: true
        );
    }

    [Fact]
    public async Task CreateArena_ShouldReturnCreatedAtAction_WhenValid()
    {
        var userId = Guid.NewGuid();
        var arenaDto = CreateSampleArenaDto();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<CreateArenaCommand>(c => c.Name == "Test Squad" && c.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateArenaResult(true, "Created", arenaDto));

        var controller = CreateController(sender, userId);
        var request = new ArenasController.CreateArenaApiRequest("Test Squad", "DotsOverall", 30);

        var actionResult = await controller.CreateArena(request, CancellationToken.None);
        var result = actionResult.Result as CreatedAtActionResult;

        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateArena_ShouldReturnBadRequest_WhenHandlerFails()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<CreateArenaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateArenaResult(false, "Name cannot be empty"));

        var controller = CreateController(sender, userId);
        var request = new ArenasController.CreateArenaApiRequest("", "DotsOverall", 30);

        var actionResult = await controller.CreateArena(request, CancellationToken.None);
        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateArena_ShouldReturnUnauthorized_WhenUnauthenticated()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, userId: null);
        var request = new ArenasController.CreateArenaApiRequest("Test", "DotsOverall", 30);

        var actionResult = await controller.CreateArena(request, CancellationToken.None);
        actionResult.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task JoinArena_ShouldReturnOk_WhenValidCode()
    {
        var userId = Guid.NewGuid();
        var arenaDto = CreateSampleArenaDto(role: "Member");
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<JoinArenaByCodeCommand>(c => c.Code == "IRON99" && c.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JoinArenaResult(true, "Joined", arenaDto));

        var controller = CreateController(sender, userId);
        var actionResult = await controller.JoinArena(new ArenasController.JoinArenaApiRequest("IRON99"), CancellationToken.None);

        var result = actionResult.Result as OkObjectResult;
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task JoinArena_ShouldReturnBadRequest_WhenCodeNotFound()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<JoinArenaByCodeCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JoinArenaResult(false, "No arena found"));

        var controller = CreateController(sender, userId);
        var actionResult = await controller.JoinArena(new ArenasController.JoinArenaApiRequest("BAD999"), CancellationToken.None);

        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMyArenas_ShouldReturnOkWithArenas()
    {
        var userId = Guid.NewGuid();
        var list = new List<ArenaDto> { CreateSampleArenaDto() };
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetUserArenasQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var controller = CreateController(sender, userId);
        var actionResult = await controller.GetMyArenas(CancellationToken.None);

        var result = actionResult.Result as OkObjectResult;
        result.Should().NotBeNull();
        result!.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task GetArena_ShouldReturnOk_WhenFound()
    {
        var userId = Guid.NewGuid();
        var arenaDto = CreateSampleArenaDto();
        var leaderboard = new ArenaLeaderboardResult(arenaDto, Array.Empty<ArenaLeaderboardEntryDto>(), null);
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetArenaLeaderboardQuery>(q => q.ArenaStringId == "test-arena" && q.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaderboard);

        var controller = CreateController(sender, userId);
        var actionResult = await controller.GetArena("test-arena", CancellationToken.None);

        var result = actionResult.Result as OkObjectResult;
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetArena_ShouldReturnNotFound_WhenNotFound()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<GetArenaLeaderboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArenaLeaderboardResult?)null);

        var controller = CreateController(sender, userId);
        var actionResult = await controller.GetArena("non-existent", CancellationToken.None);

        actionResult.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task InviteFriend_ShouldReturnOk_WhenValid()
    {
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<InviteFriendToArenaCommand>(c => c.ArenaStringId == "squad-1" && c.FriendId == friendId && c.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InviteFriendToArenaResult(true, "Sent", Guid.NewGuid()));

        var controller = CreateController(sender, userId);
        var actionResult = await controller.InviteFriend("squad-1", new ArenasController.InviteFriendApiRequest(friendId), CancellationToken.None);

        var result = actionResult.Result as OkObjectResult;
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task InviteFriend_ShouldReturnBadRequest_WhenNotFriends()
    {
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<InviteFriendToArenaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InviteFriendToArenaResult(false, "You can only invite mutual friends"));

        var controller = CreateController(sender, userId);
        var actionResult = await controller.InviteFriend("squad-1", new ArenasController.InviteFriendApiRequest(friendId), CancellationToken.None);

        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task LeaveArena_ShouldReturnOk_WhenSuccess()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<LeaveArenaCommand>(c => c.ArenaStringId == "squad-1" && c.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveArenaResult(true, "Left"));

        var controller = CreateController(sender, userId);
        var actionResult = await controller.LeaveArena("squad-1", CancellationToken.None);

        actionResult.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task LeaveArena_ShouldReturnBadRequest_WhenOwner()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<LeaveArenaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LeaveArenaResult(false, "Owners cannot leave", IsOwner: true));

        var controller = CreateController(sender, userId);
        var actionResult = await controller.LeaveArena("squad-1", CancellationToken.None);

        actionResult.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task SendKudos_ShouldReturnOk_WhenSuccessful()
    {
        var userId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<SendActivityKudosCommand>(c => c.ActivityId == activityId && c.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendActivityKudosResult(true, 5, "Cheered"));

        var controller = CreateController(sender, userId);
        var actionResult = await controller.SendKudos(activityId, CancellationToken.None);

        var result = actionResult.Result as OkObjectResult;
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SendKudos_ShouldReturnBadRequest_WhenAlreadyCheered()
    {
        var userId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<SendActivityKudosCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendActivityKudosResult(false, 3, "Already cheered"));

        var controller = CreateController(sender, userId);
        var actionResult = await controller.SendKudos(activityId, CancellationToken.None);

        actionResult.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task RespondToInvite_ShouldReturnOk_WhenSuccess()
    {
        var userId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<RespondToArenaInviteCommand>(c => c.InviteId == inviteId && c.Accept && c.UserId == userId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var controller = CreateController(sender, userId);
        var actionResult = await controller.RespondToInvite(inviteId, new ArenasController.RespondArenaInviteApiRequest(true), CancellationToken.None);

        actionResult.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RespondToInvite_ShouldReturnNotFound_WhenInviteNotFound()
    {
        var userId = Guid.NewGuid();
        var inviteId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<RespondToArenaInviteCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var controller = CreateController(sender, userId);
        var actionResult = await controller.RespondToInvite(inviteId, new ArenasController.RespondArenaInviteApiRequest(true), CancellationToken.None);

        actionResult.Should().BeOfType<NotFoundObjectResult>();
    }
}
