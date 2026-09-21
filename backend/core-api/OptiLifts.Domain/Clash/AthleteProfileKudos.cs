namespace OptiLifts.Domain.Clash;

public class AthleteProfileKudos
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public Guid TargetUserId {get; set;}
    public Guid SenderUserId {get; set;}
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
}