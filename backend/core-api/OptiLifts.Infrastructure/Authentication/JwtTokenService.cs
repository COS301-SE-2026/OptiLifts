using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OptiLifts.Application.Auth.Abstractions;
using OptiLifts.Domain.Users;

namespace OptiLifts.Infrastructure.Authentication;

public class JwtTokenService : IJwtTokenService
{
    private readonly string _secret;
    private readonly int _expiryMinutes;

    public JwtTokenService(string secret, int expiryMinutes)
    {
        _secret = secret;
        _expiryMinutes = expiryMinutes;
    }

    public string CreateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
        };

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Name, user.DisplayName));
        }

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
