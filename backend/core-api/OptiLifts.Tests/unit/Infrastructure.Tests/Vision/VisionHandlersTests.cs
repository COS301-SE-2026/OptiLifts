using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using OptiLifts.Application.Storage;
using OptiLifts.Application.Vision;
using OptiLifts.Domain.Vision;
using OptiLifts.Infrastructure.Database;
using OptiLifts.Infrastructure.Vision;
using Xunit;

namespace OptiLifts.Tests.Unit.Infrastructure.Tests.Vision;

public class VisionHandlersTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly OptiLiftsDbContext _dbContext;

    public VisionHandlersTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<OptiLiftsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new OptiLiftsDbContext(options);
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task AnalyzeVisionHandler_StoresBlobAndPersistsPendingJob()
    {
        var mockBlobService = new Mock<IBlobStorageService>();
        mockBlobService
            .Setup(b => b.SaveVisionAnalysisFramesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("http://127.0.0.1:10000/devstoreaccount1/exercises/job_123.json");

        var handler = new AnalyzeVisionHandler(_dbContext, mockBlobService.Object, serviceBusSender: null);

        var request = new VisionAnalyzeRequest
        {
            UserId = "user_abc",
            Exercise = "squat",
            View = "side",
            Frames = new List<VisionFrame>()
        };

        var jobId = await handler.Handle(new AnalyzeVisionCommand(request), CancellationToken.None);

        jobId.Should().StartWith("job_");
        var savedJob = await _dbContext.VisionAnalysisJobs.FirstOrDefaultAsync(j => j.JobId == jobId);
        savedJob.Should().NotBeNull();
        savedJob!.UserId.Should().Be("user_abc");
        savedJob.Exercise.Should().Be("squat");
        savedJob.View.Should().Be("side");
        savedJob.Status.Should().Be(VisionJobStatus.Pending);
        savedJob.BlobUrl.Should().Be("http://127.0.0.1:10000/devstoreaccount1/exercises/job_123.json");
    }

    [Fact]
    public async Task ProcessWorkerResultHandler_WhenSuccess_CallsGeminiAndMarksCompleted()
    {
        var job = new VisionAnalysisJob
        {
            JobId = "job_worker_test",
            UserId = "user_worker",
            Exercise = "deadlift",
            View = "side",
            BlobUrl = "http://example.com/blob.json",
            Status = VisionJobStatus.Pending
        };
        _dbContext.VisionAnalysisJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        var mockGemini = new Mock<IGeminiClient>();
        mockGemini
            .Setup(g => g.GenerateCoachingTipAsync("deadlift", It.IsAny<IEnumerable<VisionAnomaly>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Keep your spine neutral and drive through your heels.");

        var handler = new ProcessWorkerResultHandler(_dbContext, mockGemini.Object);

        var workerResult = new VisionWorkerResult
        {
            JobId = "job_worker_test",
            Success = true,
            DetectedAnomalies = new List<VisionAnomaly> { new("rounded lower back", 0.95) }
        };

        // act
        var handled = await handler.Handle(new ProcessWorkerResultCommand(workerResult), CancellationToken.None);

        // assert
        handled.Should().BeTrue();
        var updatedJob = await _dbContext.VisionAnalysisJobs.FirstOrDefaultAsync(j => j.JobId == "job_worker_test");
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be(VisionJobStatus.Completed);
        updatedJob.CoachSummary.Should().Be("Keep your spine neutral and drive through your heels.");
        updatedJob.DetectedAnomalies.Should().Contain(a => a.Contains("rounded lower back"));
    }

    [Fact]
    public async Task ProcessWorkerResultHandler_WhenFailed_MarksJobFailed()
    {
        var job = new VisionAnalysisJob
        {
            JobId = "job_failed_test",
            UserId = "user_fail",
            Exercise = "bench press",
            View = "front",
            BlobUrl = "http://example.com/blob.json",
            Status = VisionJobStatus.Pending
        };
        _dbContext.VisionAnalysisJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        var mockGemini = new Mock<IGeminiClient>();
        var handler = new ProcessWorkerResultHandler(_dbContext, mockGemini.Object);

        var workerResult = new VisionWorkerResult
        {
            JobId = "job_failed_test",
            Success = false
        };

        var handled = await handler.Handle(new ProcessWorkerResultCommand(workerResult), CancellationToken.None);

        handled.Should().BeTrue();
        var updatedJob = await _dbContext.VisionAnalysisJobs.FirstOrDefaultAsync(j => j.JobId == "job_failed_test");
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be(VisionJobStatus.Failed);
        mockGemini.Verify(g => g.GenerateCoachingTipAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetVisionResultHandler_WhenPending_ReturnsProcessing()
    {
        var job = new VisionAnalysisJob
        {
            JobId = "job_pending_query",
            UserId = "user_q",
            Exercise = "squat",
            View = "side",
            BlobUrl = "http://example.com/blob.json",
            Status = VisionJobStatus.Pending
        };
        _dbContext.VisionAnalysisJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        var handler = new GetVisionResultHandler(_dbContext);

        var result = await handler.Handle(new GetVisionResultQuery("job_pending_query"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().Be("processing");
        result.CoachSummary.Should().BeNull();
    }

    [Fact]
    public async Task GetVisionResultHandler_WhenCompleted_ReturnsCompletedWithCoachSummary()
    {
        var job = new VisionAnalysisJob
        {
            JobId = "job_completed_query",
            UserId = "user_q",
            Exercise = "squat",
            View = "side",
            BlobUrl = "http://example.com/blob.json",
            Status = VisionJobStatus.Completed,
            CoachSummary = "Great depth and controlled ascent!"
        };
        _dbContext.VisionAnalysisJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        var handler = new GetVisionResultHandler(_dbContext);

        var result = await handler.Handle(new GetVisionResultQuery("job_completed_query"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Status.Should().Be("completed");
        result.CoachSummary.Should().Be("Great depth and controlled ascent!");
    }

    [Fact]
    public async Task GetVisionResultHandler_WhenNotFound_ReturnsNull()
    {
        var handler = new GetVisionResultHandler(_dbContext);

        var result = await handler.Handle(new GetVisionResultQuery("non_existent_job"), CancellationToken.None);

        result.Should().BeNull();
    }
}
