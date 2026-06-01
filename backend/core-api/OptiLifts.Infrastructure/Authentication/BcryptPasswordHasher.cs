using OptiLifts.Application.Auth.Abstractions;

namespace OptiLifts.Infrastructure.Authentication;

public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; //how expensive the hashing functon will be

    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string hash, string password)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
