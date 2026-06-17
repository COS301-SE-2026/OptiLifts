using System.Security.Cryptography;
using System.Text;

namespace OptiLifts.Infrastructure.Security;

public static class TokenHelper
{

    public static string GenerateRefreshToken()
    {
        var randomB = new byte[64];
        using var rand = RandomNumberGenerator.Create(); 
        rand.GetBytes(randomB);  
        return Convert.ToBase64String(randomB);
    }
    public static string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
