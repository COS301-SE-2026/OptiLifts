using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using OptiLifts.Application.Storage;
using OptiLifts.Application.Vision;
using OptiLifts.Infrastructure.Storage;
using Xunit;

namespace OptiLifts.Tests.Infrastructure.Tests.Storage;

public class AzureBlobStorageServiceTests
{

    [Fact]
    public void Constructor_WithBlobServiceClient_InitializesSuccessfully()
    {
        //arrange
        var blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true;");

        //act
        var service = new AzureBlobStorageService(blobServiceClient);

        //assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullBlobServiceClient_ThrowsArgumentNullException()
    {
        //act
        Action act = () => new AzureBlobStorageService((BlobServiceClient)null!);

        //assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveVisionAnalysisFramesAsync_StringPayload_ThrowsWhenJobIdIsNullOrWhitespace(string? jobId)
    {
        //arrange
        var service = new AzureBlobStorageService(new BlobServiceClient("UseDevelopmentStorage=true;"));

        //act
        Func<Task> act = async () => await service.SaveVisionAnalysisFramesAsync(jobId!, "{\"frames\": []}");

        //assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveVisionAnalysisFramesAsync_StringPayload_ThrowsWhenPayloadIsNull()
    {
        //arrange
        var service = new AzureBlobStorageService(new BlobServiceClient("UseDevelopmentStorage=true;"));

        //act
        Func<Task> act = async () => await service.SaveVisionAnalysisFramesAsync("job-123", (string)null!);

        //assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task OptiVisionStorageService_DelegatesToBlobStorageService()
    {
        //arrange
        var mockBlobStorage = new Mock<IBlobStorageService>();
        var blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true;");
        mockBlobStorage
            .Setup(s => s.SaveVisionAnalysisFramesAsync("job-456", "{\"test\": true}", It.IsAny<CancellationToken>()))
            .ReturnsAsync("http://127.0.0.1:10000/devstoreaccount1/exercises/job-456.json");

        var optiVisionService = new OptiVisionStorageService(mockBlobStorage.Object, blobServiceClient);

        //act
        var result = await optiVisionService.SaveVisionAnalysisFramesAsync("job-456", "{\"test\": true}");

        //assert
        result.Should().Be("http://127.0.0.1:10000/devstoreaccount1/exercises/job-456.json");
        mockBlobStorage.Verify(s => s.SaveVisionAnalysisFramesAsync("job-456", "{\"test\": true}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OptiVisionStorageService_StageVisionAnalysisPayloadAsync_CreatesCorrectServiceBusMessage()
    {
        //arrange
        var mockBlobStorage = new Mock<IBlobStorageService>();
        var blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true;");
        const string expectedUrl = "https://saoptilifts.blob.core.windows.net/exercises/job_12345.json";

        mockBlobStorage
            .Setup(s => s.SaveVisionAnalysisFramesAsync("job_12345", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUrl);

        var optiVisionService = new OptiVisionStorageService(mockBlobStorage.Object, blobServiceClient);

        var request = new VisionAnalyzeRequest
        {
            UserId = "user_789",
            Exercise = "squat",
            View = "side",
            Frames = new List<VisionFrame>
            {
                new()
                {
                    Timestamp = 0.0,
                    Landmarks = new List<VisionLandmark>
                    {
                        new() { X = 0.5, Y = 0.6, Z = 0.1 }
                    }
                }
            }
        };

        //act
        var result = await optiVisionService.StageVisionAnalysisPayloadAsync("job_12345", request);

        //assert
        result.Should().NotBeNull();
        result.JobId.Should().Be("job_12345");
        result.Exercise.Should().Be("squat");
        result.View.Should().Be("side");
        result.BlobUrl.Should().Be(expectedUrl);
    }
}
