namespace OptiLifts.Domain.Clash;

public class ArenaMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ArenaId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Member"; //Owner, Member
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}