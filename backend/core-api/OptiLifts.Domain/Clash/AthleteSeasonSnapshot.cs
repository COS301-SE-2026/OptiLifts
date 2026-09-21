
namespace OptiLifts.Domain.Clash;

public class AthleteSeasonSnapshot
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public Guid UserId {get; set;} 
    public string SeasonKey {get; set;} = string.Empty; //eg '2026-09'
    public string DisplayName {get; set;} = string.Empty;
    public string? AvatarUrl {get; set;} 
    public string Gender {get; set;} = "Male";
    public decimal BodyweightKg {get; set;}
    public bool IsOptedIn {get; set;} = false;
    public decimal Squat1RM {get; set;} 
    public decimal Bench1RM {get; set;}
    public decimal Deadlift1RM {get; set;}
    public decimal TotalE1RM {get; set;}
    public decimal DotsScore {get; set;}
    public string Tier {get; set; } = "Bronze";
    public int TierLevel {get; set; } = 1;
    public int RankTrend {get; set;}
    public decimal WeeklyVolumeKg {get; set;}
    public DateTime LastWorkoutDate {get; set;} = DateTime.UtcNow.Date;
}