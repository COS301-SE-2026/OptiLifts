namespace OptiLifts.Domain.Users;

public class UserModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string ModelPath { get; set; } = string.Empty;
    public int TrainingSessions { get; set; }
    public DateTime TrainedAt { get; set; } = DateTime.UtcNow;
}