namespace OptiLifts.Domain.Users;
using OptiLifts.Domain.Common;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Encrypted]
    public string Email { get; set; } = string.Empty;
    public string EmailHash { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}