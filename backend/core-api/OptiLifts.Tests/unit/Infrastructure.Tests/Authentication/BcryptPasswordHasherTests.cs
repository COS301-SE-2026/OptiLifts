using FluentAssertions;
using OptiLifts.Infrastructure.Authentication;

namespace OptiLifts.Tests.Infrastructure.Tests.Authentication;

public class BcryptPasswordHasherTests
{
    [Fact]
    public void Hash_ShouldNotReturnPlainTextPassword()
    {
        var hasher = new BcryptPasswordHasher();

        var hash = hasher.Hash("P@ssw0rd!");

        hash.Should().NotBe("P@ssw0rd!");
        hash.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Verify_ShouldReturnTrueForMatchingPassword()
    {
        var hasher = new BcryptPasswordHasher();
        var password = "P@ssw0rd!";
        var hash = hasher.Hash(password);

        var result = hasher.Verify(hash, password);
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalseForNonMatchingPassword()
    {
        var hasher = new BcryptPasswordHasher();
        var hash = hasher.Hash("P@ssw0rd!");

        var result = hasher.Verify(hash, "DifferentPassword123!");
        result.Should().BeFalse();
    }
}
