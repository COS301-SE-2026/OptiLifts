using OptiLifts.Domain.Users;

namespace OptiLifts.Application.Auth.Abstractions;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
