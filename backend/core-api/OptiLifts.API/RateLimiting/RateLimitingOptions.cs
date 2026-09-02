namespace OptiLifts.API.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    public int DefaultPermitLimit { get; set; } = 100;

    public int DefaultWindowSeconds { get; set; } = 60;

    public int AuthPermitLimit { get; set; } = 15;

    public int AuthWindowSeconds { get; set; } = 60;

    public int AiPermitLimit { get; set; } = 20;

    public int AiWindowSeconds { get; set; } = 60;

    public int QueueLimit { get; set; } = 0;
}
