using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OptiLifts.Application.Vision;

namespace OptiLifts.Infrastructure.Vision;

public class GeminiClient : IGeminiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly IVisionPromptBuilder _promptBuilder;
    private readonly ILogger<GeminiClient>? _logger;
    private const string TargetModel = "gemini-3.5-flash";

    public GeminiClient(
        HttpClient httpClient,
        IConfiguration configuration,
        IVisionPromptBuilder? promptBuilder = null,
        ILogger<GeminiClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _promptBuilder = promptBuilder ?? new VisionPromptBuilder();
        _logger = logger;
        _apiKey = (Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? configuration["GEMINI_API_KEY"]
            ?? configuration["Gemini:ApiKey"]
            ?? string.Empty).Trim('"', '\'', ' ');
    }

    public Task<string> GenerateCoachingTipAsync(
        string exercise,
        IEnumerable<string> detectedAnomalies,
        CancellationToken cancellationToken = default)
    {
        var list = detectedAnomalies?
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => new VisionAnomaly(a.Trim(), 1.0));

        return GenerateCoachingTipAsync(exercise, list ?? Enumerable.Empty<VisionAnomaly>(), cancellationToken);
    }

    public async Task<string> GenerateCoachingTipAsync(
        string exercise,
        IEnumerable<VisionAnomaly> anomalies,
        CancellationToken cancellationToken = default)
    {
        var anomaliesList = GetValidAnomalies(anomalies);

        if (anomaliesList.Count == 0)
        {
            return _promptBuilder.GetPositiveReinforcement();
        }

        var prompt = _promptBuilder.BuildPrompt(exercise, anomaliesList);

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger?.LogWarning("GEMINI_API_KEY is not configured. Falling back to default coaching tip.");
            return _promptBuilder.GetFallbackCoachingTip(exercise, anomaliesList);
        }

        var requestBody = new { contents = new[] { new { parts = new[] { new { text = prompt } } } } };
        var endpoint = $"v1beta/models/{TargetModel}:generateContent?key={_apiKey}";

        var result = await ExecuteWithRetriesAsync(endpoint, requestBody, cancellationToken);
        
        if (!string.IsNullOrWhiteSpace(result))
        {
            return result;
        }
        
        return _promptBuilder.GetFallbackCoachingTip(exercise, anomaliesList);
    }

    private static List<VisionAnomaly> GetValidAnomalies(IEnumerable<VisionAnomaly> anomalies)
    {
        return anomalies?
            .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Error))
            .OrderByDescending(a => a.Severity)
            .ToList() ?? new List<VisionAnomaly>();
    }

    private async Task<string?> ExecuteWithRetriesAsync(string endpoint, object requestBody, CancellationToken cancellationToken)
    {
        int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var result = await TryExecuteOnceAsync(endpoint, requestBody, attempt, maxRetries, cancellationToken);
                if (result.IsSuccess)
                {
                    return result.Value;
                }
                
                if (result.ShouldRetry)
                {
                    await Task.Delay(result.DelayMs, cancellationToken);
                    continue;
                }
                
                return null;
            }
            catch (Exception ex) when (IsTransientException(ex))
            {
                _logger?.LogWarning(ex, "Gemini API attempt {Attempt} encountered an error.", attempt);
                if (attempt < maxRetries)
                {
                    await Task.Delay(1500 * attempt, cancellationToken);
                    continue;
                }
                return null;
            }
        }
        return null;
    }

    private async Task<(bool IsSuccess, string? Value, bool ShouldRetry, int DelayMs)> TryExecuteOnceAsync(
        string endpoint, 
        object requestBody, 
        int attempt, 
        int maxRetries, 
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger?.LogWarning("Gemini API attempt {Attempt} returned status {StatusCode}: {Error}", attempt, response.StatusCode, errorBody);

            bool retry = ShouldRetryStatus(response.StatusCode) && attempt < maxRetries;
            return (false, null, retry, 3000 * attempt);
        }

        var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
        var tip = ExtractTipFromJson(jsonDoc);
        
        return (true, tip, false, 0);
    }

    private static bool ShouldRetryStatus(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.ServiceUnavailable || statusCode == HttpStatusCode.TooManyRequests;
    }

    private static bool IsTransientException(Exception ex)
    {
        return ex is OperationCanceledException || 
               ex is TimeoutException || 
               ex is HttpRequestException || 
               ex is JsonException;
    }

    private static string? ExtractTipFromJson(JsonDocument? jsonDoc)
    {
        if (jsonDoc == null) return null;
        if (!jsonDoc.RootElement.TryGetProperty("candidates", out var candidates)) return null;
        if (candidates.GetArrayLength() == 0) return null;
        if (!candidates[0].TryGetProperty("content", out var content)) return null;
        if (!content.TryGetProperty("parts", out var parts)) return null;
        if (parts.GetArrayLength() == 0) return null;
        if (!parts[0].TryGetProperty("text", out var textProp)) return null;
        
        return textProp.GetString()?.Trim();
    }
}
