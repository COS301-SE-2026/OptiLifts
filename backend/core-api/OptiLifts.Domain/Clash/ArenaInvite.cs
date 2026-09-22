namespace OptiLifts.Domain.Clash;

public class ArenaInvite
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public string ArenaId {get; set;} = string.Empty;
    public Guid InvitedByUserId {get; set;}
    public Guid InvitedUserId {get; set;}
    public string Status {get; set;} = "Pending"; //pending, accepted, rejected
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
}