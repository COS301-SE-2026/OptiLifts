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
    private const string TargetModel = "gemini-1.5-flash";

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

    public async Task<string> GenerateCoachingTipAsync(
        string exercise,
        IEnumerable<string> detectedAnomalies,
        CancellationToken cancellationToken = default)
    {
        var anomaliesList = detectedAnomalies?
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .ToList() ?? new List<string>();

        // Handle fallbacks: If detected_anomalies is empty, generate positive reinforcement immediately
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

        try
        {
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var endpoint = $"v1beta/models/{TargetModel}:generateContent?key={_apiKey}";
            using var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger?.LogWarning("Gemini API call returned status {StatusCode}: {Error}", response.StatusCode, errorBody);
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
            _logger?.LogWarning(ex, "Gemini API call timed out or encountered an error. Applying fallback coaching tip.");
            return _promptBuilder.GetFallbackCoachingTip(exercise, anomaliesList);
        }
    }
}
