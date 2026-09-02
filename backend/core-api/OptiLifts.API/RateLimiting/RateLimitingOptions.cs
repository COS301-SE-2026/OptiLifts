namespace OptiLifts.API.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of requests allowed within the default window.
    /// </summary>
    public int DefaultPermitLimit { get; set; } = 100;

    /// <summary>
    /// Default time window in seconds.
    /// </summary>
    public int DefaultWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of requests allowed for sensitive authentication endpoints (e.g. login, register).
    /// </summary>
    public int AuthPermitLimit { get; set; } = 15;

    /// <summary>
    /// Auth time window in seconds.
    /// </summary>
    public int AuthWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of requests allowed for AI recommendation endpoints.
    /// </summary>
    public int AiPermitLimit { get; set; } = 20;

    /// <summary>
    /// AI time window in seconds.
    /// </summary>
    public int AiWindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of queued requests when limit is reached (0 = reject immediately).
    /// </summary>
    public int QueueLimit { get; set; } = 0;
}
