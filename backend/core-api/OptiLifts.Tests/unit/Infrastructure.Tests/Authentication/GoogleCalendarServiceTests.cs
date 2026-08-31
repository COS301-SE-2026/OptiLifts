using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Authentication;

namespace OptiLifts.Tests.Unit.Infrastructure.Tests.Authentication;

public sealed class GoogleCalendarServiceTests
{
    [Fact]
    public async Task GetOrCreateOptiliftsCalendarAsync_ShouldDeleteOld_WhenExistingFound()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected().SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"access_token\":\"mock_access_token\"}")
        })
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"items\":[{\"id\":\"old_cal_id\",\"summary\":\"OptiLifts\"}]}")
        })
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK
        })
        .ReturnsAsync(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("{\"id\":\"new_cal_id\"}")
        });

        var httpClient = new HttpClient(handlerMock.Object);
        var configMock = new Mock<IConfiguration>();
        var service = new GoogleCalendarService(httpClient, configMock.Object);
        var calId = await service.GetOrCreateOptiLiftsCalendarIdAsync("refresh_token", CancellationToken.None);
        calId.Should().Be("new_cal_id");

        handlerMock.Protected().Verify("SendAsync", Times.AtLeastOnce(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete && req.RequestUri != null && req.RequestUri.ToString().Contains("old_cal_id")), ItExpr.IsAny<CancellationToken>());
    }
}