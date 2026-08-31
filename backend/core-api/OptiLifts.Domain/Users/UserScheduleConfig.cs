namespace OptiLifts.Domain.Users;

public class UserScheduleConfig
{
    public Guid Id {get; set; } = Guid.NewGuid();
    public Guid UserId {get; set;}
    public bool DynamicSchedulerEnabled {get; set;} = false;
    public int MaxWorkoutsPerDay {get; set; }=1;
    public int MinMuscleRestHours {get; set;}= 48;
    public List<string> RestDays {get; set; } = new() {"Sunday"};
}