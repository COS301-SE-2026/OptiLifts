namespace OptiLifts.Domain.Clash;

public class ClashActivityKudos
{
    public Guid Id {get; set;} = Guid.NewGuid();
    public Guid ActivityId {get; set;}
    public Guid SenderUserId {get; set;}
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
}