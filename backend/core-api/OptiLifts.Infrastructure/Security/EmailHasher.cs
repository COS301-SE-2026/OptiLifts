using System.Security.Cryptography;
using System.Text;

namespace OptiLifts.Infrastructure.Security;

public static class EmailHasher
{
    public static string HashEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return string.Empty;
        var lowwerEmail = email.Trim().ToLowerInvariant();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(lowwerEmail));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}