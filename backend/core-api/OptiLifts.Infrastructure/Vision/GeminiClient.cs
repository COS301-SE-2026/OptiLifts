using System;
using System.Collections.Generic;
using System.Linq;
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
    private const string TargetModel = "gemini-3.6-flash";

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
        var anomaliesList = anomalies?
            .Where(a => a != null && !string.IsNullOrWhiteSpace(a.Error))
            .OrderByDescending(a => a.Severity)
            .ToList() ?? new List<VisionAnomaly>();

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

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        var endpoint = $"v1beta/models/{TargetModel}:generateContent?key={_apiKey}";

        int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger?.LogWarning("Gemini API attempt {Attempt} returned status {StatusCode}: {Error}", attempt, response.StatusCode, errorBody);
                    
                    // google api currently unavailable, back off for a bit
                    if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable && attempt < maxRetries)
                    {
                        await Task.Delay(1500 * attempt, cancellationToken);
                        continue;
                    }
                    return _promptBuilder.GetFallbackCoachingTip(exercise, anomaliesList);
                }

                var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
                if (jsonDoc != null &&
                    jsonDoc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var textProp))
                {
                    var tip = textProp.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(tip))
                    {
                        return tip;
                    }
                }

                return _promptBuilder.GetFallbackCoachingTip(exercise, anomaliesList);
            }
            catch (Exception ex) when (ex is OperationCanceledException or TimeoutException or HttpRequestException or JsonException)
            {
                _logger?.LogWarning(ex, "Gemini API attempt {Attempt} encountered an error.", attempt);
                if (attempt < maxRetries)
                {
                    await Task.Delay(1500 * attempt, cancellationToken);
                    continue;
                }
                return _promptBuilder.GetFallbackCoachingTip(exercise, anomaliesList);
            }
        }
        
        return _promptBuilder.GetFallbackCoachingTip(exercise, anomaliesList);
    }
}
