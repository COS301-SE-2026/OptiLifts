using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OptiLifts.API.Controllers;
using OptiLifts.Application.Vision;
using Xunit;

namespace OptiLifts.Tests.Unit.Api.Tests.Controllers;

public class VisionControllerTests
{
    private readonly Mock<ISender> _senderMock;
    private readonly VisionController _controller;

    public VisionControllerTests()
    {
        _senderMock = new Mock<ISender>();
        _controller = new VisionController(_senderMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Analyze_WhenValidRequest_ReturnsOkWithJobId()
    {
        
        var request = new VisionAnalyzeRequest
        {
            UserId = "user_123",
            Exercise = "squat",
            View = "side",
            Frames = new List<VisionFrame>()
        };

        _senderMock
            .Setup(s => s.Send(It.IsAny<AnalyzeVisionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("job_12345");

        
        var result = await _controller.Analyze(request, CancellationToken.None);

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().NotBeNull();
        _senderMock.Verify(s => s.Send(It.Is<AnalyzeVisionCommand>(c => c.Request.Exercise == "squat"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Analyze_WhenExerciseMissing_ReturnsBadRequest()
    {
        
        var request = new VisionAnalyzeRequest
        {
            UserId = "user_123",
            Exercise = ""
        };

        
        var result = await _controller.Analyze(request, CancellationToken.None);

        
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Analyze_WhenUserIdMissingAndNoAuthClaim_ReturnsBadRequest()
    {
        
        var request = new VisionAnalyzeRequest
        {
            UserId = "",
            Exercise = "squat"
        };

        
        var result = await _controller.Analyze(request, CancellationToken.None);

        
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task WorkerResult_WhenValid_ReturnsOk()
    {
        
        var request = new VisionWorkerResult
        {
            JobId = "job_123",
            Success = true,
            DetectedAnomalies = new List<VisionAnomaly> { new("knees inward", 0.9) }
        };

        _senderMock
            .Setup(s => s.Send(It.IsAny<ProcessWorkerResultCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        
        var result = await _controller.WorkerResult(request, CancellationToken.None);

        
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task WorkerResult_WhenJobNotFound_ReturnsNotFound()
    {
        
        var request = new VisionWorkerResult
        {
            JobId = "non_existent_job",
            Success = true
        };

        _senderMock
            .Setup(s => s.Send(It.IsAny<ProcessWorkerResultCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        
        var result = await _controller.WorkerResult(request, CancellationToken.None);

        
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task WorkerResult_WhenJobIdMissing_ReturnsBadRequest()
    {
        
        var request = new VisionWorkerResult
        {
            JobId = "",
            Success = true
        };

        
        var result = await _controller.WorkerResult(request, CancellationToken.None);

        
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetResult_WhenJobFound_ReturnsOkWithResult()
    {
        
        var expectedResponse = new VisionResultResponse
        {
            Status = "completed",
            CoachSummary = "Great job!"
        };

        _senderMock
            .Setup(s => s.Send(It.IsAny<GetVisionResultQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        
        var result = await _controller.GetResult("job_123", CancellationToken.None);

        
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task GetResult_WhenJobNotFound_ReturnsNotFound()
    {
        
        _senderMock
            .Setup(s => s.Send(It.IsAny<GetVisionResultQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VisionResultResponse?)null);

        
        var result = await _controller.GetResult("non_existent", CancellationToken.None);

        
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetResult_WhenJobIdEmpty_ReturnsBadRequest()
    {
        
        var result = await _controller.GetResult("", CancellationToken.None);

        
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
