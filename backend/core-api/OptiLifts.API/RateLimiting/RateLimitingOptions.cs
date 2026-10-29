namespace OptiLifts.API.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    public int DefaultPermitLimit { get; set; } = 200;

    public int DefaultWindowSeconds { get; set; } = 60;

    public int AuthPermitLimit { get; set; } = 30;

    public int AuthWindowSeconds { get; set; } = 60;

    public int AiPermitLimit { get; set; } = 40;

    public int AiWindowSeconds { get; set; } = 60;

    public int CalendarPermitLimit { get; set; } = 600;

    public int CalendarWindowSeconds { get; set; } = 60;

    public int SchedulePermitLimit { get; set; } = 600;

    public int ScheduleWindowSeconds { get; set; } = 60;

    public int QueueLimit { get; set; } = 0;
}
