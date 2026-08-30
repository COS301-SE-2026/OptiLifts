using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using OptiLifts.Application.Auth.Abstractions;
namespace OptiLifts.Infrastructure.Authentication;

public sealed class GoogleCalendarService : IGoogleCalendarService
{
    private const string BearerConst = "Bearer";
    private const string Primary = "primary";
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public GoogleCalendarService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _clientId = (Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? configuration["GOOGLE_CLIENT_ID"] ?? configuration["Authentication:Google:ClientId"] ?? string.Empty).Trim('"', '\'', ' ');
        _clientSecret = (Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? configuration["GOOGLE_CLIENT_SECRET"] ?? configuration["Authentication:Google:ClientSecret"] ?? string.Empty).Trim('"', '\'', ' ');
        Console.WriteLine($"[GoogleCalendarService Init] ClientId Length: {_clientId.Length}, ClientSecret Length: {_clientSecret.Length}");
    }

    private async Task<string> GetAccessTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;
        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
            new KeyValuePair<string, string>("grant_type", "refresh_token")
        });

        var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", requestContent, cancellationToken); //NOSONAR
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        return json?.AccessToken;
    }
    public async Task<string?> ExchangeCodeForRefreshTokenAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        });
        var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", requestContent, cancellationToken); //NOSONAR
        if (!response.IsSuccessStatusCode)
        {
            var errContent = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[Google OAuth Error] Status: {response.StatusCode}, Body: {errContent}");
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        return json?.RefreshToken;
    }

    public async Task<string> GetOrCreateOptiLiftsCalendarIdAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(refreshToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return Primary;
        }
        var request = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/calendar/v3/users/me/calendarList"); //NOSONAR
        request.Headers.Authorization = new AuthenticationHeaderValue(BearerConst, accessToken);

        var listresponse = await _httpClient.SendAsync(request, cancellationToken);
        if (listresponse.IsSuccessStatusCode)
        {
            var result = await listresponse.Content.ReadFromJsonAsync<CalendarListResponse>(cancellationToken: cancellationToken);
            var existing = result?.Items?.FirstOrDefault(c => string.Equals(c.Summary, "OptiLifts", StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(existing.Id)}");
                deleteReq.Headers.Authorization = new AuthenticationHeaderValue(BearerConst, accessToken);
                await _httpClient.SendAsync(deleteReq, cancellationToken);
            }
        }

        var createrequest = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/calendar/v3/calendars");//NOSONAR
        createrequest.Headers.Authorization = new AuthenticationHeaderValue(BearerConst, accessToken);
        createrequest.Content = JsonContent.Create(new
        {
            summary = "OptiLifts",
            description = "OptiLifts Workout Schedule Calendar"
        });
        var createresponse = await _httpClient.SendAsync(createrequest, cancellationToken);
        if (!createresponse.IsSuccessStatusCode)
        {
            return Primary;
        }
        var calendar = await createresponse.Content.ReadFromJsonAsync<CalendarItem>(cancellationToken: cancellationToken);
        return calendar?.Id ?? Primary;
    }

    public async Task<string?> CreateEventAsync(string refreshToken, string calendarId, GoogleCalendarEventDto eventDto, CancellationToken cancellationToken = default)
    {
        var accessToken = await GetAccessTokenAsync(refreshToken, cancellationToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }
        var startTimeutc = DateTime.SpecifyKind(eventDto.StartTime, DateTimeKind.Utc);
        var endTimeUtc = startTimeutc.AddMinutes(eventDto.DurationMinutes);

        var payload = new
        {
            summary = eventDto.Summary,
            description = eventDto.Description,
            colorId = "11",
            start = new
            {
                dateTime = startTimeutc.ToString("yyyy-MM-ddTHH:mm:ssZ")
            },
            end = new
            {
                dateTime = endTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        };

        var targetcalendar = string.IsNullOrWhiteSpace(calendarId) ? Primary : calendarId;
        var request = new HttpRequestMessage(HttpMethod.Post, $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(targetcalendar)}/events");
        request.Headers.Authorization = new AuthenticationHeaderValue(BearerConst, accessToken);
        request.Content = JsonContent.Create(payload);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound || response.StatusCode == System.Net.HttpStatusCode.Gone)
        {
            targetcalendar = await GetOrCreateOptiLiftsCalendarIdAsync(refreshToken, cancellationToken);

            var retry = new HttpRequestMessage(HttpMethod.Post, $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(targetcalendar)}/events");
            retry.Headers.Authorization = new AuthenticationHeaderValue(BearerConst, accessToken);
            retry.Content = JsonContent.Create(payload);
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }
        var createdEvent = await response.Content.ReadFromJsonAsync<GoogleEventResponse>(cancellationToken: cancellationToken);
        return createdEvent?.Id;
    }

    public async Task DeleteEventAsync(string refreshToken, string calendarId, string eventId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(calendarId))
        {
            return;
        }
        try
        {
            var accessToken = await GetAccessTokenAsync(refreshToken, cancellationToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return;
            }
            var request = new HttpRequestMessage(HttpMethod.Delete, $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(calendarId)}/events/{Uri.EscapeDataString(eventId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue(BearerConst, accessToken);

            await _httpClient.SendAsync(request, cancellationToken);
        }
        catch
        {
            //ignore google calendar's errors
        }

    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken
    );
    private sealed record CalendarListResponse(
        [property: JsonPropertyName("items")] List<CalendarItem>? Items
    );
    private sealed record CalendarItem(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("summary")] string Summary
    );
    private sealed record GoogleEventResponse(
        [property: JsonPropertyName("id")] string Id
    );
}

