using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using OptiLifts.Domain.Users;
using OptiLifts.Infrastructure.Authentication;

namespace OptiLifts.Tests.Infrastructure.Tests.Authentication;

public class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_ShouldReturnATokenString()
    {
        var service = CreateService();
        var token = service.CreateToken(CreateUser());

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CreateToken_ShouldIncludeExpectedClaims()
    {
        var user = CreateUser();
        var service = CreateService();
        var token = service.CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().ContainSingle(c => c.Type == JwtRegisteredClaimNames.Name && c.Value == user.DisplayName);
    }

    [Fact]
    public void CreateToken_ShouldValidateWithConfiguredSecret()
    {
        const string secret = "unit-test-secret-unit-test-secret-unit-test-secret";
        var service = new JwtTokenService(secret, 60);
        var token = service.CreateToken(CreateUser());
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
        };
        var tokenHandler = new JwtSecurityTokenHandler();

        tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

        validatedToken.Should().BeOfType<JwtSecurityToken>();
    }

    private static JwtTokenService CreateService(string? secret = null, int expiryMinutes = 60)
    {
        return new JwtTokenService(
            secret ?? "unit-test-secret-unit-test-secret-unit-test-secret",
            expiryMinutes);
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = "jane.doe@example.com",
            DisplayName = "Jane Doe"
        };
    }
}
