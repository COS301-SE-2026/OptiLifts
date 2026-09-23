using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using OptiLifts.Infrastructure.Vision;
using Xunit;

namespace OptiLifts.Tests.Unit.Infrastructure.Tests.Vision;

public class GeminiClientTests
{
    [Fact]
    public async Task GenerateCoachingTipAsync_WhenAnomaliesEmpty_ReturnsPositiveReinforcement()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };
        var configMock = new Mock<IConfiguration>();
        var client = new GeminiClient(httpClient, configMock.Object);

        var result = await client.GenerateCoachingTipAsync("squat", new List<string>());

        result.Should().Be("Clean reps! Your form looks solid, keep it up.");
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GenerateCoachingTipAsync_WhenApiKeyMissing_ReturnsFallbackTip()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["GEMINI_API_KEY"]).Returns(string.Empty);
        var client = new GeminiClient(httpClient, configMock.Object);

        var result = await client.GenerateCoachingTipAsync("deadlift", new[] { "rounded back" });

        result.Should().Contain("deadlift");
        result.Should().Contain("rounded back");
    }

    [Fact]
    public async Task GenerateCoachingTipAsync_WhenGeminiReturnsSuccess_ReturnsParsedCoachingTip()
    {
        const string geminiResponseJson = @"{
            ""candidates"": [
                {
                    ""content"": {
                        ""parts"": [
                            {
                                ""text"": ""Keep your chest high and drive through your heels. Ensure your knees track in line with your toes throughout the descent.""
                            }
                        ]
                    }
                }
            ]
        }";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(geminiResponseJson, System.Text.Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["GEMINI_API_KEY"]).Returns("mock-api-key");
        var client = new GeminiClient(httpClient, configMock.Object);

        var result = await client.GenerateCoachingTipAsync("squat", new[] { "knees caving in" });

        result.Should().Be("Keep your chest high and drive through your heels. Ensure your knees track in line with your toes throughout the descent.");
    }

    [Fact]
    public async Task GenerateCoachingTipAsync_WhenGeminiFails_ReturnsGracefulFallback()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server Error")
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["GEMINI_API_KEY"]).Returns("mock-api-key");
        var client = new GeminiClient(httpClient, configMock.Object);

        var result = await client.GenerateCoachingTipAsync("bench press", new[] { "uneven lockout" });

        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("bench press");
    }
}
