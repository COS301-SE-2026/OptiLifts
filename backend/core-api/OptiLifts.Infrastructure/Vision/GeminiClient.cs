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
    private readonly ILogger<GeminiClient>? _logger;
    private const string TargetModel = "gemini-1.5-flash";

    public GeminiClient(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiClient>? logger = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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

        // Phase 5 fallback: if detected_anomalies is empty, generate positive reinforcement
        if (anomaliesList.Count == 0)
        {
            return "Clean reps! Your form looks solid, keep it up.";
        }

        var commaSeparated = string.Join(", ", anomaliesList);
        var prompt = $"User {exercise} errors: {commaSeparated}. Write a concise, actionable 2-sentence coaching tip encouraging the user and instructing proper form.";

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger?.LogWarning("GEMINI_API_KEY is not configured. Falling back to default coaching tip.");
            return $"Great effort on your {exercise}! Focus on correcting {commaSeparated} to maintain proper form.";
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
                return $"Great effort on your {exercise}! Keep working on your form and focus on controlled reps.";
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

            return $"Great effort on your {exercise}! Focus on fixing {commaSeparated} to ensure safe and effective movement.";
        }
        catch (Exception ex) when (ex is OperationCanceledException or HttpRequestException or JsonException)
        {
            _logger?.LogWarning(ex, "Gemini API call encountered an error. Applying fallback coaching tip.");
            return $"Great effort on your {exercise}! Stay mindful of {commaSeparated} and keep your reps controlled.";
        }
    }
}
