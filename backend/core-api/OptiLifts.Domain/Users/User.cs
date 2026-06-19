using OptiLifts.Domain.Common;
namespace OptiLifts.Domain.Users;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Encrypted]
    public string Email { get; set; } = string.Empty;
    public string EmailHash { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    [Encrypted]
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public int Level { get; set; }
    [Encrypted]
    public string? Weight { get; set; }
    [Encrypted]
    public string? Height { get; set; }
    [Encrypted]
    public string? Sex { get; set; }
    //a string converted on the API layer e.g 
    //public enum Sex { Male, Female, Other, PreferNotToSay }
    [Encrypted]
    public string? DateOfBirth { get; set; }
    public string? Bio { get; set; }
    public bool Metric { get; set; }
    public bool LightTheme { get; set; }
}