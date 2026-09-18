using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Clash.Friends;
using OptiLifts.Application.Clash.Friends.Commands;
using OptiLifts.Application.Clash.Friends.Queries;
using OptiLifts.Domain.Users;

namespace OptiLifts.Tests.Api.Tests.Controllers;

public sealed class FriendsControllerTests
{
    private const string defcode = "ABC123";
    private const string auth = "TestAuth";

    private static FriendsController CreateController(Mock<ISender> sender, Guid? userId = null)
    {
        var controller = new FriendsController(sender.Object);
        var context = new DefaultHttpContext();
        if (userId.HasValue)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, userId.Value.ToString())
            };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, auth));
        }
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
        return controller;
    }

    private static FriendDto createFriend(
        Guid id,
        string name = "Daenerys Stormborn",
        string code = defcode
    ) => new(
        id, name, "DS", null, code, 350.5m, "Gold"
    );
    private static FriendRequestDto createRequest(
        Guid id,
        Guid fromId,
        string name = "Daenerys Stormborn",
        string code = "CD5678"
    ) => new(id, fromId, name, "DS", null, code, DateTime.UtcNow);

    //get /friends/code - pass
    [Fact]
    public async Task GetMyCode_ShouldReturnOk_WhenCodeExists()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetMyFriendCodeQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
        .ReturnsAsync(defcode);

        var controller = CreateController(sender, userId);
        var result = await controller.GetMyCode(CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
    }
    [Fact]
    public async Task GetMyCode_ShouldReturnNotFound_WhenCodeisNull()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetMyFriendCodeQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
        .ReturnsAsync((string?)null);

        var controller = CreateController(sender, userId);
        var result = await controller.GetMyCode(CancellationToken.None);
        result.Result.Should().BeOfType<NotFoundResult>();
    }
    [Fact]
    public async Task GetMyCode_ShouldReturnUnauthorised_WhenNoUserid()
    {
        var sender = new Mock<ISender>();

        var controller = CreateController(sender, userId: null);
        var result = await controller.GetMyCode(CancellationToken.None);
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }
    //get /friends - all pass
    [Fact]
    public async Task GetFriends_ShouldReturnOkWithFriends_WhenAuthorised()
    {
        var userId = Guid.NewGuid();
        var friends = new List<FriendDto> {
            createFriend(Guid.NewGuid())
        };
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetFriendsListQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
        .ReturnsAsync(friends);

        var controller = CreateController(sender, userId);
        var result = await controller.GetFriends(CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
        var okresult = (OkObjectResult)result.Result;
        okresult.Value.Should().BeEquivalentTo(friends);
    }
    [Fact]
    public async Task GetFriends_SHouldReturnUnauthorised_WhenUseridMissing()
    {
        var sender = new Mock<ISender>();

        var controller = CreateController(sender, userId: null);
        var result = await controller.GetFriends(CancellationToken.None);
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    //get /friends/requests - all pass
    [Fact]
    public async Task GetRequests_SHouldReturnOkWithRequests_WhenAuthorised()
    {
        var userId = Guid.NewGuid();
        var incoming = new List<FriendRequestDto>{
            createRequest(Guid.NewGuid(), Guid.NewGuid())
        };
        var outgoing = new List<FriendRequestDto>();
        var pending = new PendingFriendRequestsResult(incoming, outgoing);

        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<GetPendingFriendRequestsQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
        .ReturnsAsync(pending);

        var controller = CreateController(sender, userId);
        var result = await controller.GetRequests(CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
        var okresult = (OkObjectResult)result.Result;
        okresult.Value.Should().BeEquivalentTo(pending);
    }
    [Fact]
    public async Task GetRequest_SHouldReturnUnauthorised_WhenUseridMissing()
    {
        var sender = new Mock<ISender>();

        var controller = CreateController(sender, userId: null);
        var result = await controller.GetRequests(CancellationToken.None);
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    //post /friends/requests
    [Fact]
    public async Task SendRequest_ShouldReturnOk_WhenReqSucceeds()
    {
        var userId = Guid.NewGuid();
        var sendresult = new SendFriendRequestResult(true, "Friend Request sent", Guid.NewGuid());
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<SendFriendRequestCommand>(q => q.UserId == userId && q.FriendCode == defcode), It.IsAny<CancellationToken>()))
        .ReturnsAsync(sendresult);

        var controller = CreateController(sender, userId);
        var result = await controller.SendRequest(new FriendsController.SendFriendRequestApiRequest(defcode), CancellationToken.None);
        result.Result.Should().BeOfType<OkObjectResult>();
        var okresult = (OkObjectResult)result.Result;
        okresult.Value.Should().BeEquivalentTo(sendresult);
    }
    [Fact]
    public async Task SendRequest_ShouldReturnBadRequest_WhenRequestFails()
    {
        var userId = Guid.NewGuid();
        var sendresult = new SendFriendRequestResult(false, "Cannot add yourself");
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<SendFriendRequestCommand>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(sendresult);

        var controller = CreateController(sender, userId);
        var result = await controller.SendRequest(new FriendsController.SendFriendRequestApiRequest(defcode), CancellationToken.None);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
    [Fact]
    public async Task SendRequest_ShouldReturnUnauthorised_WhenuserIdMissing()
    {
        var sender = new Mock<ISender>();

        var controller = CreateController(sender, userId: null);
        var result = await controller.SendRequest(new FriendsController.SendFriendRequestApiRequest(defcode), CancellationToken.None);
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    //post /friends/requests/{id}/respond
    [Fact]
    public async Task RespondToRequest_ShouldReturnOk_WhenResponseSucceeeds()
    {
        var userId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<RespondToFriendRequestCommand>(q => q.UserId == userId && q.RequestId == reqId && q.Accept), It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

        var controller = CreateController(sender, userId);
        var result = await controller.RespondToRequest(reqId, new FriendsController.RespondFriendRequestApiRequest(true), CancellationToken.None);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task RespondToRequest_ShouldReturnNotFound_WhenRequestNotFoundorProcessed()
    {
        var userId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<RespondToFriendRequestCommand>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);

        var controller = CreateController(sender, userId);
        var result = await controller.RespondToRequest(reqId, new FriendsController.RespondFriendRequestApiRequest(false), CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RespondToRequest_ShouldUnauthorised_WhenUseridMissing()
    {
        var sender = new Mock<ISender>();

        var controller = CreateController(sender, userId: null);
        var result = await controller.RespondToRequest(Guid.NewGuid(), new FriendsController.RespondFriendRequestApiRequest(true), CancellationToken.None);
        result.Should().BeOfType<UnauthorizedResult>();
    }

    //post /friends/requests/reject-all
    [Fact]
    public async Task RejectAllRequests_ShouldReturnOk_WhenAuthorised()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<RejectAllFriendRequestsCommand>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
        .ReturnsAsync(3);

        var controller = CreateController(sender, userId);
        var result = await controller.RejectAllRequests(CancellationToken.None);
        result.Should().BeOfType<OkObjectResult>();
    }
    [Fact]
    public async Task RejectAllRequests_ShouldReturnUnauthorised_WhenUserIdMissing()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, userId: null);
        var result = await controller.RejectAllRequests(CancellationToken.None);
        result.Should().BeOfType<UnauthorizedResult>();
    }

    //delete /friends/{friendid}
    [Fact]
    public async Task RemoveFriend_ShouldReturnNoContent_WhenFriendReemoved()
    {
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.Is<RemoveFriendCommand>(q => q.UserId == userId && q.FriendId == friendId), It.IsAny<CancellationToken>()))
        .ReturnsAsync(true);

        var controller = CreateController(sender, userId);
        var result = await controller.RemoveFriend(friendId, CancellationToken.None);
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RemoveFriend_ShouldReturnNotFound_WhenFriendshipNotFound()
    {
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send(It.IsAny<RemoveFriendCommand>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(false);

        var controller = CreateController(sender, userId);
        var result = await controller.RemoveFriend(friendId, CancellationToken.None);
        result.Should().BeOfType<NotFoundObjectResult>();
    }
    [Fact]
    public async Task RemoveFriend_ShouldReturnUnauthorised_WhenUseridMissing()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender, userId: null);
        var result = await controller.RemoveFriend(Guid.NewGuid(), CancellationToken.None);
        result.Should().BeOfType<UnauthorizedResult>();
    }

}