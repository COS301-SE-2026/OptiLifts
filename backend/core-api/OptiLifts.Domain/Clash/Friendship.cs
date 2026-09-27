namespace OptiLifts.Domain.Clash;

public class Friendship
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId1 { get; set; }
    public Guid UserId2 { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}